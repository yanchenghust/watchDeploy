# watchDeploy
监控本地文件变动，并自动部署到服务器。    
本地开发时，经常需要将代码部署到测试环境。phpstorm自带的deploy功能可以很方便的将代码通过sftp部署到测试机。    
当然，一般开发环境都会有relay跳板机，必须通过跳板机才能登录开发机或测试机。这个不难解决，通过ssh隧道（或者叫端口映射、端口转发）将本地的一个自定义端口通过跳板机映射到测试机，再将phpstorm的部署端口设置为本地端口即可。    
但是，运维同学为了安全因素考虑，禁用了端口映射。部署到测试机只能通过rz名来来上传压缩包。每次需要将整个项目文件打包上传，再解包，很是麻烦。    
本程序通过本地文件变动的监控，将有变动的文件自动上传到测试机。当然，测试机需要一个接收的端口，为了方便起见，使用http（即普通的web请求）来上传文件。    
***
# 运行时截图
![image](https://github.com/yanchenghust/watchDeploy/raw/image.png)    
***
# 文件变动监控
采用C#的FileSystemWatcher来监控文件变动并上传。    
为了灵活的扩展（比如想同时监控N个工作目录），采用配置文件的方式来增加FileSystemWatcher对象。程序在启动时，根据配置文件的内容来动态的添加监控目录及选项。    
***
# 配置文件格式
[deploy]    
watchdir : /path/to/watch    
receiver : http://test.my.com/server_file_receiver.php    
filter : *.php    
ison : 1    
[.map]    
local : /path/to/local1    
remote : /path/to/remote1    
[.map]    
local : /path/to/local2    
remote : /path/to/remote2    

可以有多个[deploy]项，各个字段的意义如下：    
watchdir: 要监控的目录。    
receiver: 该目录对应的接收url。需要php/server_file_receiver.php脚本部署到服务器，receiver填写能访问到这个接口的url即可。    
filter: 对应C# FileSystemWatcher的Filter字段    
每个[.map]对应一个本地和远程的目录映射关系，可以填写多个[.map]项。    
local : 本地目录    
remote : 远程目录    

***
# 适用：
本地为Windows操作系统，远程测试机为Linux操作系统。    

