import importlib.util
from pathlib import Path
import os
from unittest import TestCase,main
from unittest.mock import Mock,patch

SPEC=importlib.util.spec_from_file_location('owned_signal',Path(__file__).parents[1]/'TestHosts/Signal-OwnedConsoleProcess.py')
MODULE=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(MODULE)

class OwnedConsoleGuardTests(TestCase):
    def kernel(self,target,members):
        kernel=Mock()
        calls=0
        def listing(array,length):
            nonlocal calls
            calls+=1
            values=[os.getpid()] if calls==1 else members
            for index,value in enumerate(values):array[index]=value
            return len(values)
        kernel.GetConsoleProcessList.side_effect=listing
        kernel.AttachConsole.return_value=True
        kernel.FreeConsole.return_value=True
        kernel.SetConsoleCtrlHandler.return_value=True
        kernel.GenerateConsoleCtrlEvent.return_value=True
        return kernel

    def test_inherited_helper_console_is_detached_before_exact_owned_signal(self):
        target=123456;kernel=self.kernel(target,[target,os.getpid()])
        with patch.object(MODULE.ctypes,'WinDLL',return_value=kernel):
            result=MODULE.signal_owned_console(target)
        self.assertTrue(result['ctrlCSent'])
        kernel.AttachConsole.assert_called_once_with(target)
        kernel.GenerateConsoleCtrlEvent.assert_called_once_with(0,0)
        self.assertEqual(2,kernel.FreeConsole.call_count)

    def test_unexpected_console_member_prevents_every_signal(self):
        target=123456;kernel=self.kernel(target,[target,os.getpid(),789012])
        with patch.object(MODULE.ctypes,'WinDLL',return_value=kernel),self.assertRaises(RuntimeError):
            MODULE.signal_owned_console(target)
        kernel.GenerateConsoleCtrlEvent.assert_not_called()
        kernel.SetConsoleCtrlHandler.assert_not_called()

    def test_self_or_invalid_pid_never_calls_native_console_api(self):
        for target in [0,-1,os.getpid()]:
            with self.subTest(target=target),patch.object(MODULE.ctypes,'WinDLL') as loader,self.assertRaises(RuntimeError):
                MODULE.signal_owned_console(target)
            loader.assert_not_called()

if __name__=='__main__':main()
