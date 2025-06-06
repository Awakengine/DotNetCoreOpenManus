# OpenManus.Web - .NET Core MVC Web应用程序

## 项目概述

这是一个基于.NET Core MVC的OpenManus Web应用程序，提供完整的用户管理、聊天会话和AI代理功能，并集成了Swagger API文档。

## 功能特性

### ✅ 已实现功能

1. **用户管理系统**
   - 用户登录和认证
   - 游客用户创建
   - JWT令牌认证
   - 会话管理

2. **聊天系统**
   - 创建和管理聊天会话
   - 发送和接收消息
   - 支持认证用户和游客用户
   - 会话历史记录

3. **AI代理集成**
   - 多种AI代理支持
   - 代理配置管理
   - 智能对话处理

4. **API文档**
   - Swagger UI集成
   - 完整的API文档
   - JWT认证支持
   - 交互式API测试

5. **Web界面**
   - 响应式设计
   - 现代化UI
   - 用户管理界面
   - 代理管理界面

## 技术栈

- **.NET Core** - 现代化的.NET技术栈
- **ASP.NET Core MVC** - Web应用程序框架
- **JWT Authentication** - 安全认证
- **Swagger/OpenAPI** - API文档
- **Bootstrap** - 前端UI框架
- **C#** - 强类型编程语言

## 项目结构

```
OpenManus.Web/
├── Controllers/
│   ├── AgentController.cs        # AI代理控制器
│   ├── ChatController.cs         # 聊天控制器
│   ├── HomeController.cs         # 主页控制器
│   ├── UserController.cs         # 用户控制器
│   └── UserManagementController.cs # 用户管理控制器
├── Models/
│   ├── ChatMessage.cs            # 聊天消息模型
│   ├── ChatSessionInfo.cs        # 聊天会话模型
│   ├── ErrorViewModel.cs         # 错误视图模型
│   └── UserModels.cs             # 用户模型
├── Services/
│   ├── IJwtService.cs            # JWT服务接口
│   ├── IUserService.cs           # 用户服务接口
│   ├── JwtService.cs             # JWT服务实现
│   ├── SessionManagementService.cs # 会话管理服务
│   └── UserService.cs            # 用户服务实现
├── Views/                        # MVC视图
├── wwwroot/                      # 静态资源
├── Data/                         # 数据存储
├── Middleware/                   # 中间件
└── Program.cs                    # 应用程序入口
```

## 快速开始

### 环境要求

- .NET Core SDK
- 支持现代浏览器

### 安装步骤

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd OpenManusDotNetCore
   ```

2. **恢复依赖**
   ```bash
   cd OpenManus.Web
   dotnet restore
   ```

3. **构建项目**
   ```bash
   dotnet build
   ```

4. **运行应用程序**
   ```bash
   dotnet run
   ```

5. **访问应用程序**
   - Web应用: `http://localhost:5180`
   - Swagger API文档: `http://localhost:5180/swagger`

## API文档

### Swagger UI

项目集成了Swagger UI，提供完整的API文档和交互式测试界面：

- **访问地址**: `http://localhost:5180/swagger`
- **OpenAPI规范**: `http://localhost:5180/swagger/v1/swagger.json`

### 主要API端点

#### 用户管理 API
- `POST /api/user/login` - 用户登录
- `POST /api/user/guest` - 创建游客用户

#### 聊天管理 API
- `GET /api/chat/sessions` - 获取用户聊天会话
- `POST /api/chat/sessions` - 创建新聊天会话
- `POST /api/chat/send` - 发送聊天消息

#### AI代理 API
- `GET /api/agent` - 获取可用代理列表
- 其他代理相关操作

### 认证方式

- **JWT Bearer Token**: 用于认证用户
- **Session Cookie**: 用于游客用户
- **匿名访问**: 部分端点支持匿名访问

## 配置说明

### JWT配置

在 `appsettings.json` 中配置JWT设置：

```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "OpenManus.Web",
    "Audience": "OpenManus.Web.Users",
    "ExpiryMinutes": 60
  }
}
```

### 会话配置

```json
{
  "SessionOptions": {
    "IdleTimeout": "00:30:00",
    "Cookie": {
      "HttpOnly": true,
      "IsEssential": true
    }
  }
}
```

## 部署说明

### 开发环境

```bash
# 启动开发服务器
dotnet run

# 指定端口
dotnet run --urls="http://localhost:5180"
```

### 生产环境

```bash
# 发布应用
dotnet publish -c Release -o ./publish

# 运行发布版本
cd publish
dotnet OpenManus.Web.dll
```

### Docker部署

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OpenManus.Web/OpenManus.Web.csproj", "OpenManus.Web/"]
RUN dotnet restore "OpenManus.Web/OpenManus.Web.csproj"
COPY . .
WORKDIR "/src/OpenManus.Web"
RUN dotnet build "OpenManus.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OpenManus.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenManus.Web.dll"]
```

## 开发指南

### 添加新的API端点

1. 在相应的Controller中添加新方法
2. 添加适当的HTTP属性和路由
3. 添加Swagger文档注释
4. 配置认证和授权

### 扩展功能

项目架构支持以下扩展：

1. **数据库集成**
   - 添加Entity Framework Core
   - 配置数据库连接
   - 实现数据持久化

2. **实时通信**
   - 集成SignalR
   - 实现实时聊天
   - 推送通知

3. **缓存系统**
   - 添加Redis缓存
   - 会话状态管理
   - 性能优化

4. **日志和监控**
   - 结构化日志
   - 性能监控
   - 错误追踪

## 故障排除

### 常见问题

1. **端口冲突**
   - 修改 `launchSettings.json` 中的端口配置
   - 使用 `--urls` 参数指定端口

2. **JWT认证失败**
   - 检查JWT密钥配置
   - 验证Token格式和有效期

3. **Swagger无法访问**
   - 确保在开发环境中运行
   - 检查Swagger中间件配置

## 许可证

本项目采用MIT许可证。详见LICENSE文件。

---

**项目状态**: ✅ 生产就绪  
**版本**: 1.0.0  
**最后更新**: 2024年

