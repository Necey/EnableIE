using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.CSharp.RuntimeBinder;
using System.Linq;

namespace EnableIE
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        //兼容性列表在注册表中的位置，注意：不同位的操作系统可能不同，请注意测试。
        private const string CLEARABLE_LIST_DATA = @"Software\Microsoft\Internet Explorer\BrowserEmulation\ClearableListData";
        private const string USERFILTER = "UserFilter";
        private readonly byte[] header = new byte[] { 0x41, 0x1F, 0x00, 0x00, 0x53, 0x08, 0xAD, 0xBA };
        private readonly byte[] delim_a = new byte[] { 0x01, 0x00, 0x00, 0x00 };
        private readonly byte[] delim_b = new byte[] { 0x0C, 0x00, 0x00, 0x00 };
        private readonly byte[] checksum = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
        private readonly byte[] filler = BitConverter.GetBytes(DateTime.Now.ToBinary());//new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        private readonly string url32bit = "https://soft.huoru.cn/ieframe.dll";
        private readonly string url64bit = "https://soft.huoru.cn/64/ieframe.dll";
        private readonly string filePath32bit = @"C:\Windows\System32\ieframe.dll";
        private readonly string filePath64bit = @"C:\Windows\SysWOW64\ieframe.dll";
        private string endURL = "error";



        /// <summary>
        /// 初次启动加载项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            Text = "当前版本：" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "仅限Windows10和Windows11使用";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\Main", true))
            {
                // 设置“启用第三方浏览器扩展”值为 0（禁用）
                key.SetValue("Enable Browser Extensions", "no", RegistryValueKind.String);
            }
            //禁用成功
            SetPopupMGR(GetPopupMGR());
            // 判断操作系统是否为Windows 10或Windows 11
            if (IsWindows11() || IsWindows10())
            {
                FrmTips.ShowTipsSuccess(this, "当前系统可以使用");
            }
            else
            {
                FrmTips.ShowTips(this, "当前系统不可用！！！", 10000, true, ContentAlignment.MiddleCenter, null, TipsSizeMode.Medium, new Size(300, 50), TipsState.Error);
                return;
            }
            // 使用 RemoveRegistrySubKey 方法删除指定的注册表子键
            string registryPath1 = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Ext\CLSID";
            string subKeyName1 = "{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}";

            RemoveRegistrySubKey(registryPath1, subKeyName1);

            string registryPath2 = @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Policies\Ext\CLSID";
            string subKeyName2 = "{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}";

            RemoveRegistrySubKey(registryPath2, subKeyName2);
        }

        /// <summary>
        /// 判断操作系统是否为Windows 11
        /// </summary>
        /// <returns></returns>
        public bool IsWindows11()
        {
            const string subkey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            const string name = "ProductName";

            RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey);
            if (key != null)
            {
                string productName = key.GetValue(name)?.ToString();
                return productName?.Contains("Windows 11") == true;
            }

            return false;
        }

        /// <summary>
        /// 判断操作系统是否为Windows 10
        /// </summary>
        /// <returns></returns>
        public bool IsWindows10()
        {
            const string subkey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
            const string name = "CurrentBuildNumber";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey))
            {
                if (key != null)
                {
                    string buildNumber = key.GetValue(name)?.ToString();

                    if (!string.IsNullOrEmpty(buildNumber) && int.TryParse(buildNumber, out int build))
                    {
                        return build >= 10240;
                    }
                }
            }

            return false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            endURL = ChkSiteIsOK(textBox1.Text);
            if (endURL.Equals("error"))
            {
                FrmTips.ShowTips(this, "您输入了一个错误的网址，请检查", 5000, true, ContentAlignment.MiddleCenter, null, TipsSizeMode.Medium, new Size(300, 50), TipsState.Error);
                return;
            }
            if (checkBox1.Checked)
            {
                AddNewSiteToCompatibilityViewList(RemoveProtocolAndPort(endURL));
            }
            if (checkBox2.Checked)
            {
                CreateIEShortcutOnDesktop();
            }
            if (IsWindows11())
            {
                string vbsCode = $@"
                                    Dim wsh, ie
                                      Set wsh = CreateObject(""wscript.shell"")
                                      Set ie = CreateObject(""InternetExplorer.Application"")
                                      URL = ""{endURL}""
                                      ie.Visible = True
                                      ie.navigate URL
                                    ";

                string script = string.Format(vbsCode, endURL);
                try
                {
                    // 执行VBScript脚本
                    Process p = new Process();
                    p.StartInfo.FileName = "cscript.exe";  // 指定要执行的程序
                    p.StartInfo.Arguments = "/nologo";   // 告诉cscript.exe不显示Logo信息
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.UseShellExecute = false;
                    p.Start();       // 启动进程
                    p.StandardInput.Write(script);  // 将VBScript代码写入进程的标准输入流
                    p.StandardInput.Close();        // 关闭标准输入流
                    p.WaitForExit();   // 等待进程退出
                }
                catch (Exception ex)
                {
                    FrmTips.ShowTipsWarning(this, ex.Message+"1");
                }
            }
            else if (IsWindows10())
            {
                // 指定要打开的 URL 和命令行参数
                string url = endURL + "/#";
                string args = "bing -Embedding";
                try
                {
                    // 启动 Internet Explorer
                    Process.Start("iexplore.exe", url + " " + args);
                }
                catch (Exception ex)
                {
                    FrmTips.ShowTipsWarning(this, ex.Message+"2");
                }
            }
            //如果是更新后的浏览器无法启动，则替换文件以达到重新启用IE浏览器的目的
            if (ChkFileVersion(filePath32bit, "11.0.19041.3271"))
            {
                ReplaceFileWithAdminPermission(filePath32bit, url32bit);
                RestoreFileOwnerWithAdminPermission(filePath32bit);
                ReplaceFileWithAdminPermission(filePath64bit, url64bit);
                RestoreFileOwnerWithAdminPermission(filePath64bit);
            }
        }

        /// <summary>
        /// 得到已经存在的所有兼容网站列表，如果没有，则返回空数组。
        /// </summary>
        /// <returns></returns>
        private string[] GetDomains()
        {
            string[] domains = { };
            using (RegistryKey regkey = Registry.CurrentUser.OpenSubKey(CLEARABLE_LIST_DATA))
            {
                byte[] filter = regkey?.GetValue(USERFILTER) as byte[];
                if (filter != null)
                {
                    domains = GetDomains(filter);
                }
            }
            return domains;
        }


        /// <summary>
        /// 从byte数组中分析所有网站名称
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string[] GetDomains(byte[] filter)
        {
            List<string> domains = new List<string>();
            int length;
            int offset_filter = 24;
            int totalSize = filter.Length;
            while (offset_filter < totalSize)
            {
                length = BitConverter.ToUInt16(filter, offset_filter + 16);
                domains.Add(System.Text.Encoding.Unicode.GetString(filter, 16 + 2 + offset_filter, length * 2));
                offset_filter += 16 + 2 + length * 2;
            }
            return domains.ToArray();
        }

        /// <summary>
        /// 向兼容性列表中添加一个网站
        /// </summary>
        /// <param name="domain"></param>
        private void AddNewSiteToCompatibilityViewList(String domain)
        {
            String[] domains = GetDomains();
            if (domains.Length > 0)
            {
                if (ArrayContains(domains, domain))
                {
                    return;
                }
                else
                {
                    domains = AddToArray(domains, domain);
                }
            }
            else
            {
                domains = new String[] { domain };
            }

            int count = domains.Length;
            byte[] entries = new byte[0];
            foreach (String d in domains)
            {
                byte[] domainEntry = GetDomainEntry(d);
                entries = Combine(entries, domainEntry);
            }
            byte[] regbinary = Combine(header, BitConverter.GetBytes(count));
            regbinary = Combine(regbinary, checksum);
            regbinary = Combine(regbinary, delim_a);
            regbinary = Combine(regbinary, BitConverter.GetBytes(count));
            regbinary = Combine(regbinary, entries);
            Registry.CurrentUser.CreateSubKey(CLEARABLE_LIST_DATA).SetValue(USERFILTER, regbinary);
        }


        /// <summary>
        /// 得到一个网站在兼容性列表中的数据，跟GetRemovedValue类似
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        private byte[] GetDomainEntry(String domain)
        {
            byte[] tmpbinary = new byte[0];
            byte[] length = BitConverter.GetBytes((UInt16)domain.Length);
            byte[] data = System.Text.Encoding.Unicode.GetBytes(domain);
            tmpbinary = Combine(tmpbinary, delim_b);
            tmpbinary = Combine(tmpbinary, filler);
            tmpbinary = Combine(tmpbinary, delim_a);
            tmpbinary = Combine(tmpbinary, length);
            tmpbinary = Combine(tmpbinary, data);
            return tmpbinary;
        }

        //把两个byte[]数组合并在一起
        private byte[] Combine(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, c, 0, a.Length);
            Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }

        public static bool ArrayContains(string[] array, string searchValue)
        {
            foreach (string value in array)
            {
                if (value == searchValue)
                {
                    return true;
                }
            }
            return false;
        }

        public static string[] AddToArray(string[] inputArray, string newItem)
        {
            string[] newArray = new string[inputArray.Length + 1];
            inputArray.CopyTo(newArray, 0);
            newArray[inputArray.Length] = newItem;
            return newArray;
        }

        
        //校验是否启用窗口阻止程序
        public bool GetPopupMGR()
        {
            string regPath = @"Software\Microsoft\Internet Explorer\New Windows";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
            {
                if (key != null)
                {
                    string popupMgrValue = key.GetValue("PopupMgr")?.ToString();
                    if (popupMgrValue != null)
                    {
                        if (int.TryParse(popupMgrValue, out int intValue))
                        {
                            return (intValue == 1);
                        }
                        else if (string.Equals(popupMgrValue, "yes", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // 设置弹出窗口阻止程序设置（关闭）
        public void SetPopupMGR(bool value)
        {
            try
            {
                // 设置弹出窗口阻止程序设置
                string regPath = @"Software\Microsoft\Internet Explorer\New Windows";
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    if (key != null)
                    {
                        int intValue = value ? 1 : 0;
                        key.SetValue("PopupMgr", intValue, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常，例如记录日志或抛出自定义异常
                Console.WriteLine("An error occurred while setting PopupMgr value: " + ex.Message);
            }
        }

        /// <summary>
        /// 请求管理员权限并替换文件
        /// </summary>
        /// <param name="filePath">被替换文件</param>
        /// <param name="url">下载的URL地址</param>
        private void ReplaceFileWithAdminPermission(string filePath, string url)
        {
            // 请求管理员权限并修改文件权限
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c takeown /f {filePath} && icacls {filePath} /grant {Environment.UserName}:(F)";
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas"; // 请求管理员权限
            startInfo.CreateNoWindow = true; // 隐藏窗口
            startInfo.WindowStyle = ProcessWindowStyle.Hidden; // 隐藏窗口

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                try
                {
                    string destinationPath = filePath + ".bak"; // 添加 .bak 后缀
                    System.IO.File.Move(filePath, destinationPath);
                    bool downloadSucceeded = false;

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            using (WebClient client = new WebClient())
                            {
                                client.DownloadFileCompleted += (sender, e) =>
                                {
                                    if (e.Error == null)
                                    {
                                        downloadSucceeded = true;
                                        RestoreFileOwnerWithAdminPermission(filePath);
                                        System.IO.File.Delete(destinationPath);
                                    }
                                    else
                                    {
                                        FrmTips.ShowTipsError(this,"文件下载失败：" + e.Error.Message);
                                        // 下载失败后将文件移动回原来的位置，恢复文件权限
                                        string originalPath = filePath.Replace(".bak", ""); // 去除 .bak 后缀
                                        System.IO.File.Move(filePath, originalPath);
                                        RestoreFileOwnerWithAdminPermission(filePath);
                                    }
                                };

                                client.DownloadFileAsync(new Uri(url), filePath);
                            }

                        }
                        catch (Exception ex)
                        {
                            FrmTips.ShowTipsWarning(this, "文件下载失败：" + ex.Message);
                            // 下载失败后将文件移动回原来的位置
                            string originalPath = filePath.Replace(".bak", ""); // 去除 .bak 后缀
                            System.IO.File.Move(filePath, originalPath);
                        }
                    }).ContinueWith(task =>
                    {

                        while (!downloadSucceeded)
                        {

                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                catch (Exception ex)
                {
                    FrmTips.ShowTipsWarning(this, "文件操作失败：" + ex.Message);
                }

            }
            else
            {
                FrmTips.ShowTipsError(this, "命令执行失败！请以管理员身份运行本程序！");
            }
        }

        /// <summary>
        /// 恢复文件所有者为TrustedInstaller
        /// </summary>
        /// <param name="filePath">被恢复文件所有者的文件</param>
        private void RestoreFileOwnerWithAdminPermission(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c icacls {filePath} /setowner \"NT SERVICE\\TrustedInstaller\" && icacls {filePath} /grant:r \"administrator:(RX)\"";
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas"; // 请求管理员权限
            startInfo.CreateNoWindow = true; // 隐藏窗口
            startInfo.WindowStyle = ProcessWindowStyle.Hidden; // 隐藏窗口

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                FrmTips.ShowTipsSuccess(this,"文件所有者已恢复为 TrustedInstaller，权限已设置！");
            }
            else
            {
                FrmTips.ShowTipsError(this, "命令执行失败！请以管理员身份运行本程序！");
            }
        }

        /// <summary>
        /// 检测ieframe.dll版本是否为屏蔽IE的新版本
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        private bool ChkFileVersion(string filePath, string targetVersion)
        {
            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                string fileVersion = fileVersionInfo.ProductVersion;
                Version currentVersion = new Version(fileVersion);
                Version target = new Version(targetVersion);

                if (currentVersion > target)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 处理其他异常
                FrmTips.ShowTipsError(this, "程序发生异常：" + ex.Message);
            }

            return false;
        }


        /// <summary>
        /// 移除协议、端口号和路径部分以添加至兼容性视图中
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string RemoveProtocolAndPort(string url)
        {
            string modifiedURL = url.Replace("http://", "").Replace("https://", "");

            int portIndex = modifiedURL.LastIndexOf(":");
            if (portIndex != -1)
            {
                modifiedURL = modifiedURL.Substring(0, portIndex);
            }

            int pathIndex = modifiedURL.IndexOf("/");
            if (pathIndex != -1)
            {
                modifiedURL = modifiedURL.Substring(0, pathIndex);
            }

            return modifiedURL;
        }

        /// <summary>
        /// 检测URL的合法性，如果合法则返回URL，非法则返回error
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string ChkSiteIsOK(string url)
        {
            try
            {
                // 检查URL是否以"http://"或"https://"开头
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "http://" + url; // 默认使用http协议
                }

                // 创建一个HttpWebRequest对象
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                // 发送请求并获取响应
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    // 检查HTTP状态码
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return url;
                    }
                    else
                    {
                        return "网站不可用，状态码：" + response.StatusCode;
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    Console.WriteLine("网站不可用，状态码：" + errorResponse.StatusCode);

                    // 如果是HTTP请求失败，尝试使用HTTPS协议
                    if (errorResponse.StatusCode >= HttpStatusCode.BadRequest && errorResponse.StatusCode < HttpStatusCode.InternalServerError &&
                        url.StartsWith("http://"))
                    {
                        url = "https://" + url.Substring(7); // 修改URL协议为HTTPS
                        Console.WriteLine("尝试使用HTTPS协议重新请求");

                        // 递归调用ChkSiteIsOK方法，尝试HTTPS请求
                        return ChkSiteIsOK(url);
                    }
                }
                else
                {
                    FrmTips.ShowTips(this, "网络连接失败：" + ex.Message, 5000, true, ContentAlignment.MiddleCenter, null, TipsSizeMode.Medium, new Size(300, 50), TipsState.Error);
                }
            }
            catch (Exception ex)
            {
                // 处理其他异常
                FrmTips.ShowTips(this, "程序发生异常：" + ex.Message, 5000, true, ContentAlignment.MiddleCenter, null, TipsSizeMode.Medium, new Size(300, 50), TipsState.Error);
            }
            return "error"; // 无法连接到网站
        }

        /// <summary>
        /// 创建IE浏览器快捷方式
        /// </summary>
        public void CreateIEShortcutOnDesktop()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = $"{desktopPath}\\Internet Explorer.lnk";
            string internetExplorerPath = Environment.ExpandEnvironmentVariables("%ProgramFiles%\\Internet Explorer\\iexplore.exe");

            // 创建快捷方式对象
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            // 设置快捷方式属性
            shortcut.TargetPath = internetExplorerPath;
            shortcut.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)+ "\\Internet Explorer";
            shortcut.Description = "Internet Explorer";
            shortcut.IconLocation = $"{internetExplorerPath},0";
            shortcut.Save();
        }

        /// <summary>
        /// 删除IE To Edge BHO
        /// </summary>
        /// <param name="registryPath"></param>
        /// <param name="subKeyName"></param>
        public void RemoveRegistrySubKey(string registryPath, string subKeyName)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $"delete \"{registryPath}\" /v \"{subKeyName}\" /f",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
