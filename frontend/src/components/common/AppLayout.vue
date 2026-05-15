<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/modules/auth'
import { ElMessageBox } from 'element-plus'

const router = useRouter()
const auth = useAuthStore()

const menuItems = [
  { path: '/dashboard', icon: 'Odometer', label: '仪表盘' },
  { path: '/records', icon: 'List', label: '账单' },
  { path: '/records/recurring', icon: 'Timer', label: '周期账单' },
  { path: '/budget', icon: 'TrendCharts', label: '预算' },
  { path: '/stats/month', icon: 'PieChart', label: '月度统计' },
  { path: '/stats/year', icon: 'DataLine', label: '年度统计' },
  { path: '/ai/analysis', icon: 'MagicStick', label: 'AI 分析' },
  { path: '/accounts', icon: 'CreditCard', label: '账户' },
  { path: '/category', icon: 'Grid', label: '分类' },
  { path: '/settings', icon: 'Setting', label: '设置' },
]

async function handleLogout() {
  await ElMessageBox.confirm('确认退出登录？', '提示', { type: 'warning' })
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <el-container class="layout">
    <el-aside width="200px" class="sidebar">
      <div class="logo">💰 记账精灵</div>
      <el-menu router :default-active="$route.path" background-color="#1a1a2e" text-color="#aaa" active-text-color="#fff">
        <el-menu-item v-for="item in menuItems" :key="item.path" :index="item.path">
          <el-icon><component :is="item.icon" /></el-icon>
          <span>{{ item.label }}</span>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="header">
        <span class="greeting">你好，{{ auth.user?.nickname || auth.user?.username }}</span>
        <el-button link @click="handleLogout">退出登录</el-button>
      </el-header>
      <el-main class="main">
        <RouterView />
      </el-main>
    </el-container>
  </el-container>
</template>

<style scoped>
.layout { height: 100vh; }
.sidebar { background: #1a1a2e; display: flex; flex-direction: column; }
.logo { color: #fff; font-size: 18px; font-weight: 700; padding: 20px 16px; border-bottom: 1px solid #2a2a4e; }
.header { display: flex; align-items: center; justify-content: flex-end; gap: 12px; border-bottom: 1px solid var(--el-border-color); }
.greeting { font-size: 14px; color: var(--el-text-color-secondary); }
.main { background: #f5f7fa; overflow-y: auto; }
</style>
