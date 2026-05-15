import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/modules/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/login', name: 'login', component: () => import('@/views/auth/LoginView.vue'), meta: { public: true } },
    { path: '/register', name: 'register', component: () => import('@/views/auth/RegisterView.vue'), meta: { public: true } },
    {
      path: '/',
      component: () => import('@/components/common/AppLayout.vue'),
      children: [
        { path: '', redirect: '/dashboard' },
        { path: 'dashboard', name: 'dashboard', component: () => import('@/views/dashboard/DashboardView.vue') },
        { path: 'records', name: 'records', component: () => import('@/views/records/RecordsView.vue') },
        { path: 'records/add', name: 'records-add', component: () => import('@/views/records/RecordFormView.vue') },
        { path: 'records/:id/edit', name: 'records-edit', component: () => import('@/views/records/RecordFormView.vue') },
        { path: 'records/recurring', name: 'records-recurring', component: () => import('@/views/records/RecurringView.vue') },
        { path: 'budget', name: 'budget', component: () => import('@/views/budget/BudgetView.vue') },
        { path: 'stats/month', name: 'stats-month', component: () => import('@/views/stats/MonthStatsView.vue') },
        { path: 'stats/year', name: 'stats-year', component: () => import('@/views/stats/YearStatsView.vue') },
        { path: 'accounts', name: 'accounts', component: () => import('@/views/accounts/AccountsView.vue') },
        { path: 'category', name: 'category', component: () => import('@/views/category/CategoryView.vue') },
        { path: 'settings', name: 'settings', component: () => import('@/views/settings/SettingsView.vue') },
        { path: 'ai/analysis', name: 'ai-analysis', component: () => import('@/views/ai/AiAnalysisView.vue') },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: '/dashboard' },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.public && !auth.isLoggedIn) return '/login'
  if (to.meta.public && auth.isLoggedIn) return '/dashboard'
})

export default router
