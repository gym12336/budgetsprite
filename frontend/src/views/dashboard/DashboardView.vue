<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { dashboardApi } from '@/api/stats'
import { useRouter } from 'vue-router'

const router = useRouter()
const data = ref<any>(null)
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try { data.value = await dashboardApi.get() }
  finally { loading.value = false }
})
</script>

<template>
  <div v-loading="loading">
    <h2 style="margin-bottom:20px">仪表盘</h2>

    <el-row :gutter="16" v-if="data">
      <el-col :span="6">
        <el-card shadow="never">
          <div class="stat-label">本月收入</div>
          <div class="stat-value income">¥{{ data.monthIncome?.toFixed(2) }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never">
          <div class="stat-label">本月支出</div>
          <div class="stat-value expense">¥{{ data.monthExpense?.toFixed(2) }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never">
          <div class="stat-label">本月结余</div>
          <div class="stat-value">¥{{ data.monthBalance?.toFixed(2) }}</div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card shadow="never">
          <div class="stat-label">上月支出</div>
          <div class="stat-value secondary">¥{{ data.lastMonthExpense?.toFixed(2) }}</div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16" style="margin-top:16px" v-if="data">
      <!-- 预算进度 -->
      <el-col :span="12">
        <el-card shadow="never" header="预算进度">
          <div v-if="data.budgets?.some((b:any) => b.remaining < 0)" style="margin-bottom:10px">
            <el-tag type="danger">⚠️ 有 {{ data.budgets.filter((b:any) => b.remaining < 0).length }} 项预算超支</el-tag>
          </div>
          <div v-for="b in data.budgets" :key="b.id" class="budget-row">
            <span :style="{color: b.remaining < 0 ? '#f56c6c' : b.used/b.amount >= 0.8 ? '#e6a23c' : '', fontWeight: b.remaining < 0 ? '700' : '400'}">
              {{ b.categoryName || '总预算' }}
            </span>
            <el-progress
              :percentage="Math.min(100, Math.round((b.used / b.amount) * 100))"
              :status="b.remaining < 0 ? 'exception' : b.used / b.amount >= 0.8 ? 'warning' : ''"
              style="flex:1;margin:0 12px"
            />
            <span class="budget-text" :style="{color: b.remaining < 0 ? '#f56c6c' : '#888'}">
              {{ Number(b.used).toFixed(0) }} / {{ Number(b.amount).toFixed(0) }}
            </span>
          </div>
          <el-empty v-if="!data.budgets?.length" description="暂无预算" />
        </el-card>
      </el-col>

      <!-- 最近记录 -->
      <el-col :span="12">
        <el-card shadow="never" header="最近账单">
          <div v-for="r in data.recentRecords" :key="r.id" class="record-row">
            <span>{{ r.categoryName }}</span>
            <span class="record-note">{{ r.note || r.tags }}</span>
            <span :class="r.type === 1 ? 'income' : 'expense'">
              {{ r.type === 1 ? '+' : '-' }}{{ r.amount?.toFixed(2) }}
            </span>
          </div>
          <el-empty v-if="!data.recentRecords?.length" description="暂无记录" />
        </el-card>
      </el-col>
    </el-row>

    <el-button
      type="primary"
      :icon="'Plus'"
      style="position:fixed;right:32px;bottom:32px;border-radius:50%;width:56px;height:56px;font-size:24px"
      circle
      @click="router.push('/records/add')"
    />
  </div>
</template>

<style scoped>
.stat-label { font-size: 13px; color: #888; margin-bottom: 8px; }
.stat-value { font-size: 24px; font-weight: 700; }
.income { color: #67c23a; }
.expense { color: #f56c6c; }
.secondary { color: #909399; }
.budget-row { display: flex; align-items: center; margin-bottom: 12px; font-size: 13px; }
.budget-text { min-width: 80px; text-align: right; font-size: 12px; color: #888; }
.record-row { display: flex; align-items: center; padding: 8px 0; border-bottom: 1px solid #f0f0f0; font-size: 13px; }
.record-note { flex: 1; color: #aaa; padding: 0 8px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
