一款解决Windows10和Windows11系统无法使用的工具

主要功能：
•重点是免重启！！！
•屏蔽IEtoEdge BHO
•替换旧版本ieframe.dll
•关闭弹窗拦截（默认即关闭，如有需要可以手动恢复开启状态）
•创建IE浏览器快捷方式到当前用户桌面
•添加指定站点到兼容性视图列表

运行环境：
•支持Win10和Win11系统使用，使用.net framework 4.0开发

已知问题：
•提权替换ieframe.dll后恢复权限不完整，当前用户所属用户组还是具有完全控制权限
•下载替换的时候如果强行结束程序则会导致再次使用时获取不到版本号而需要手工恢复.bak备份文件
