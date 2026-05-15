<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { statsApi } from '@/api/stats'
import * as echarts from 'echarts'

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1)
const data = ref<any>(null)
const loading = ref(false)
const pieRef = ref<HTMLElement>()
const barRef = ref<HTMLElement>()
let pieChart: echarts.ECharts | null = null
let barChart: echarts.ECharts | null = null

async function fetchStats() {
  loading.value = true
  try {
    data.value = await statsApi.month(year.value, month.value)
    await nextTick()
    renderCharts()
  } finally {
    loading.value = false
  }
}

function renderCharts() {
  if (!data.value) return
  if (pieRef.value && data.value.categoryStats?.length) {
    pieChart = pieChart ?? echarts.init(pieRef.value)
    pieChart.setOption({
      tooltip: { trigger: 'item', formatter: '{b}: ¥{c} ({d}%)' },
      legend: { bottom: 0, type: 'scroll' },
      series: [{ type: 'pie', radius: ['40%', '70%'],
        data: data.value.categoryStats.map((c: any) => ({ name: c.categoryName, value: Number(c.amount), itemStyle: { color: c.color || undefined } })),
        label: { show: false }, emphasis: { label: { show: true, fontSize: 14, fontWeight: 'bold' } } }],
    })
  }
  if (barRef.value) {
    barChart = barChart ?? echarts.init(barRef.value)
    const days = data.value.dailyStats
    barChart.setOption({
      tooltip: { trigger: 'axis' },
      legend: { data: ['支出', '收入'] },
      grid: { left: '3%', right: '4%', bottom: '10%', containLabel: true },
      xAxis: { type: 'category', data: days.map((d: any) => d.date), axisLabel: { rotate: 45 } },
      yAxis: { type: 'value' },
      series: [
        { name: '支出', type: 'bar', data: days.map((d: any) => Number(d.expense)), itemStyle: { color: '#f56c6c' } },
        { name: '收入', type: 'bar', data: days.map((d: any) => Number(d.income)), itemStyle: { color: '#67c23a' } },
      ],
    })
  }
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
        <el-col :span="8"><el-card shadow="never">
          <div class="stat-label">本月收入</div>
          <div class="stat-value" style="color:#67c23a">¥{{ Number(data?.totalIncome??0).toFixed(2) }}</div>
        </el-card></el-col>
        <el-col :span="8"><el-card shadow="never">
          <div class="stat-label">本月支出</div>
          <div class="stat-value" style="color:#f56c6c">¥{{ Number(data?.totalExpense??0).toFixed(2) }}</div>
        </el-card></el-col>
        <el-col :span="8"><el-card shadow="never">
          <div class="stat-label">本月结余</div>
          <div class="stat-value">¥{{ Number(data?.balance??0).toFixed(2) }}</div>
          <div style="font-size:12px;color:#999;margin-top:4px">上月支出 ¥{{ Number(data?.lastMonthExpense??0).toFixed(2) }}</div>
        </el-card></el-col>
      </el-row>
      <el-row :gutter="16">
        <el-col :span="10">
          <el-card shadow="never" header="支出分类占比">
            <el-empty v-if="!data?.categoryStats?.length" description="暂无支出数据" />
            <div v-else ref="pieRef" style="height:300px" />
          </el-card>
        </el-col>
        <el-col :span="14">
          <el-card shadow="never" header="每日收支趋势">
            <div ref="barRef" style="height:300px" />
          </el-card>
        </el-col>
      </el-row>
    </div>
  </div>
</template>

<style scoped>
.stat-label { font-size:13px; color:#888; margin-bottom:8px; }
.stat-value { font-size:22px; font-weight:700; }
</style>
