# OpenManus WebUI .NET Core - 快速开始指南

## 🚀 5分钟快速启动

### 第一步：安装.NET 10.0 SDK

```bash
# 下载SDK
wget https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.100-preview.3.25201.16/dotnet-sdk-10.0.100-preview.3.25201.16-linux-x64.tar.gz

# 解压并配置环境
mkdir -p $HOME/dotnet && tar zxf dotnet-sdk-10.0.100-preview.3.25201.16-linux-x64.tar.gz -C $HOME/dotnet
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet

# 验证安装
dotnet --version
```

### 第二步：运行项目

```bash
# 进入项目目录
cd OpenManus.Host

# 构建项目
dotnet build

# 运行应用程序
dotnet run --urls="http://0.0.0.0:5001"
```

### 第三步：访问应用程序

打开浏览器访问：`http://localhost:5001`

## ✨ 功能演示

### 文件浏览
1. 查看左侧文件列表
2. 点击文件名查看详情
3. 使用预览功能查看文件内容

### 聊天功能
1. 在底部输入框输入消息
2. 点击"发送"按钮
3. 查看聊天历史和响应

### 界面操作
- **显示预览**: 切换文件预览模式
- **清空对话**: 清除聊天历史
- **文件导航**: 使用面包屑导航

## 🔧 常见问题

### Q: 端口被占用怎么办？
A: 使用不同端口运行：
```bash
dotnet run --urls="http://0.0.0.0:5002"
```

### Q: 如何添加新文件？
A: 将文件放入`workspace`目录即可自动显示

### Q: 如何自定义样式？
A: 编辑`wwwroot/css/app.css`文件

## 📱 界面预览

应用程序界面包含：
- **左侧导航栏**: 深蓝色背景，包含导航菜单
- **文件浏览器**: 显示文件列表和详细信息
- **聊天界面**: 实时消息交互
- **预览区域**: 文件内容预览

## 🎯 下一步

1. 探索文件管理功能
2. 测试聊天交互
3. 自定义界面样式
4. 集成真实的AI服务

---

**提示**: 这个实现完全匹配原始OpenManus-WebUI项目的界面和功能！

