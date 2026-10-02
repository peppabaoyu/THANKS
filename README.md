# 歇歇：分享与 GitHub 自动更新

当前版本：3.0.0。你的更新仓库预设为 **peppabaoyu/THANKS**。

## 先分享给朋友

把同文件夹里的 **XieXie-Setup.exe** 发给朋友，双击后点「安装 / 更新」即可。安装在当前 Windows 用户目录，无需管理员权限，桌面会出现「歇歇」。适用于 Windows 10 / 11，需 .NET Framework 4.8（多数新版 Windows 已具备）。

安装包包含程序、高清缩放配置、七个角色、字体和使用说明；不包含你的照片、设置和统计。数据各自保存在使用者电脑的 `%LOCALAPPDATA%\XieXie`。再次安装会保留数据。

如果只想解压运行，用 XieXie-Portable.zip；请把整个文件夹解压，保留 exe.config 文件与程序放在一起，否则会失去新版高清缩放配置。

这是未购买数字签名证书的个人软件，Windows 可能显示“未知发布者”。不要关闭系统安全防护；确认来自你的发布页面，必要时核对 SHA256SUMS.txt。

## 第一次创建仓库：网页操作

1. 登录 GitHub，打开 https://github.com/new 。Owner 选 **peppabaoyu**，Repository name 填 **THANKS**。
2. 选择 **Public**，勾选 Add a README file，然后 Create repository。这里用公开仓库是为了让朋友无需登录或令牌即可检查更新。
3. 下载并解压 **THANKS-GitHub-source.zip**。这是专门整理的源码包，不是安装包。不要上传当前聊天的整个工作目录，也不要上传 AppData 里的个人数据。
4. 在仓库主页点 **Add file → Upload files**，把解压后的 `work`、`outputs` 文件夹、README.md 和 .gitignore 拖进去，Commit changes。保持目录结构；不要把源代码压缩包作为一个文件上传。
5. `.github` 是点开头的文件夹，网页拖拽可能遗漏。最稳妥的方法：点 **Add file → Create new file**，文件名填写 `.github/workflows/release.yml`，把源码包里对应文件的全文复制进去，Commit changes。确认仓库里确实有这个路径。
6. 打开仓库 **Actions**，如有启用提示，允许本仓库运行工作流。选择左侧 **Build and release XieXie**，点 **Run workflow**，分支选 main，再点绿色 Run workflow。
7. 等此次运行变成绿色，打开仓库 **Releases**。会自动生成 **v3.0.0**，附件包含 `XieXie-Setup.exe`、`XieXie-Portable.zip` 和 `SHA256SUMS.txt`。如果失败，打开红色的步骤查看错误，不要重复发布同一个版本号。

固定分享地址（首次发布完成后才可用）：

- 发布页：https://github.com/peppabaoyu/THANKS/releases/latest
- 最新安装包：https://github.com/peppabaoyu/THANKS/releases/latest/download/XieXie-Setup.exe

GitHub 自带的 “Source code (zip)” 是源码，朋友应下载上面的 **XieXie-Setup.exe**。

## 以后发布更新

1. 修改程序后，把更新过的源码放回仓库。
2. 修改 `work/AppVersion.cs` 中两处版本号，例如把 `3.0.0.0` 都改成 `3.0.1.0`。版本必须递增，不能反复用 v3.0.0 覆盖不同内容。
3. 再执行 **Actions → Build and release XieXie → Run workflow**。流程会构建、运行检查、校验安装包内容，并自动发布 `v3.0.1`。
4. 朋友下一次打开歇歇，会自动检查更新；也可以从主页面底部「更新与 GitHub」点「检查更新」。发现新版后，由使用者确认下载安装，软件校验 GitHub 提供的 SHA-256 后才运行安装包，保存数据、关闭旧版并启动新版。

如果习惯 Git，也可推送与 AppVersion.cs 一致的版本标签（如 `v3.0.1`）触发同一流程。不要同时手动创建同名 Release 再运行流程，以免发布冲突。

## “自动更新”的范围

- 已实现：启动自动检查、手动检查、版本比较、下载校验、确认安装、保留本机数据。
- 不会在工作过程中静默强制重启，也不会自动生成新的程序代码。
- 仓库目前尚未创建，因此尚未验证线上发布链路。第一次成功发布后，这套更新地址才会生效；当前安装包本身可直接使用。
- 网络无法访问 GitHub、仓库设为私有、没有正式 Release、Release 缺少 `XieXie-Setup.exe` 或校验信息时，检查会失败；自动检查静默略过，手动检查会显示原因。草稿和预发布版本不会作为稳定更新。
- 「更新与 GitHub」可关闭启动检查。不需要把 GitHub 密码或个人访问令牌写入软件。

## 电视与多屏使用

主页面采用按内容撑开的高度、自动换行，以及根据窗口宽度切换的一至三列提醒卡片。支持 Windows 高 DPI 设置和跨屏 DPI 调整；高度不足时可滚动，最大化可充分利用电视空间。请保留安装目录中的 `歇歇.exe.config`。

已在本机检查窄 / 宽窗口与模拟缩放；无法直接在你的 65 寸电视上做实机验证。如果仍有异常，提供电视分辨率、Windows「缩放」百分比和新版截图即可进一步定位。

## 官方参考

- Windows 高清缩放：https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms
- GitHub Releases：https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases
- 网页上传文件：https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository
- 工作流触发：https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow
- Release API 与附件校验信息：https://docs.github.com/en/rest/releases/releases
