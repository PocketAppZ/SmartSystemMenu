﻿using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SmartSystemMenu.Native;

namespace SmartSystemMenu.Extensions
{
    static class ProcessExtensions
    {
        public static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            try
            {
                return process.MainModule.FileName;
            }
            catch
            {
                var fileNameBuilder = new StringBuilder(buffer);
                var bufferLength = (uint)fileNameBuilder.Capacity + 1;
                return NativeMethods.QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) ? fileNameBuilder.ToString() : null;
            }
        }

        public static IList<IntPtr> GetWindowHandles(this Process process)
        {
            var handles = new List<IntPtr>();
            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.EnumThreadWindows(thread.Id, (hwnd, lParam) => { handles.Add(hwnd); return true; }, 0);
            }
            return handles;
        }

        public static Process GetParentProcess(this Process process)
        {
            var pbi = new PROCESS_BASIC_INFORMATION();
            int returnLength;
            var status = NativeMethods.NtQueryInformationProcess(process.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
            {
                return null;
            }

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch
            {
                return null;
            }
        }

        public static void Suspend(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = NativeMethods.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                NativeMethods.SuspendThread(pOpenThread);
            }
        }

        public static void Resume(this Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = NativeMethods.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                NativeMethods.ResumeThread(pOpenThread);
            }
        }

        public static bool IsSuspended(this Process process)
        {
            return process.Threads[0].ThreadState == ThreadState.Wait
                && process.Threads[0].WaitReason == ThreadWaitReason.Suspended;
        }
    }
}
