<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { statsApi } from '@/api/stats'

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1)
const data = ref<any>(null)
const loading = ref(false)

async function fetchStats() {
  loading.value = true
  try { data.value = await statsApi.month(year.value, month.value) }
  finally { loading.value = false }
}

onMounted(fetchStats)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">月度统计</h2>
      <el-date-picker
        :model-value="`${year}-${String(month).padStart(2,'0')}`"
        type="month" value-format="YYYY-MM"
        @update:model-value="(v:string) => { const [y,m]=v.split('-'); year=+y; month=+m; fetchStats() }"
      />
    </div>
    <div v-loading="loading">
      <el-row :gutter="16" style="margin-bottom:16px">
        <el-col :span="8">
          <el-card shadow="never">
            <div class="stat-label">本月收入</div>
            <div class="stat-value" style="color:#67c23a">¥{{ data?.totalIncome?.toFixed(2) ?? '-' }}</div>
          </el-card>
        </el-col>
        <el-col :span="8">
          <el-card shadow="never">
            <div class="stat-label">本月支出</div>
            <div class="stat-value" style="color:#f56c6c">¥{{ data?.totalExpense?.toFixed(2) ?? '-' }}</div>
          </el-card>
        </el-col>
        <el-col :span="8">
          <el-card shadow="never">
            <div class="stat-label">本月结余</div>
            <div class="stat-value">¥{{ data?.balance?.toFixed(2) ?? '-' }}</div>
          </el-card>
        </el-col>
      </el-row>
      <el-card shadow="never" header="支出分类占比" style="margin-bottom:16px">
        <el-empty v-if="!data?.categoryStats?.length" description="暂无数据" />
        <div v-for="item in data?.categoryStats" :key="item.categoryId" class="cat-item">
          <span style="min-width:80px;font-size:13px">{{ item.categoryName }}</span>
          <el-progress :percentage="Math.round(item.percent)" :color="item.color||'#409eff'" style="flex:1;margin:0 12px" />
          <span style="min-width:80px;text-align:right;font-size:13px;color:#666">¥{{ item.amount?.toFixed(2) }}</span>
        </div>
      </el-card>
      <el-card shadow="never" header="每日收支趋势（ECharts 图表待接入）">
        <el-empty description="此处将接入 ECharts 柱状图" />
      </el-card>
    </div>
  </div>
</template>

<style scoped>
.stat-label { font-size:13px; color:#888; margin-bottom:8px; }
.stat-value { font-size:22px; font-weight:700; }
.cat-item { display:flex; align-items:center; margin-bottom:12px; }
</style>
