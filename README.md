# 记账精灵 BudgetSprite

个人记账 Web 系统 · Vue 3 + ASP.NET Core 8 · 前后端分离

## 项目结构

```
budgetsprite/
├── BudgetSprite.Api/   # 后端 ASP.NET Core 8 WebAPI
└── frontend/           # 前端 Vue 3 + Vite
```

## 快速启动

### 前提条件
- .NET 8 SDK
- Node.js 18+
- MySQL 8

### 后端

```bash
cd BudgetSprite.Api
# 1. 修改 appsettings.json 中的数据库连接串和 JWT Key
# 2. 运行（开发模式自动执行 Migration）
dotnet run
# Swagger: http://localhost:5000/swagger
```

### 前端

```bash
cd frontend
npm install
npm run dev
# 访问: http://localhost:5173
```

## 技术栈

| 端 | 技术 |
|---|---|
| 前端 | Vue 3 + Vite + Element Plus + ECharts + Pinia + Axios |
| 后端 | ASP.NET Core 8 WebAPI + EF Core 8 + MySQL 8 + JWT |

## 分工

| 成员 | 职责 |
|---|---|
| A | 后端：项目脚手架、JWT 认证、Records CRUD、导入导出 |
| B | 后端：分类/账户/预算 API、统计聚合、周期账单 |
| C | 前端：登录注册、仪表盘、账单列表、记账表单 |
| D | 前端：ECharts 图表、预算页、分类账户设置页 |
