<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { statsApi } from '@/api/stats'
import * as echarts from 'echarts'

const year = ref(new Date().getFullYear())
const data = ref<any>(null)
const loading = ref(false)
const lineRef = ref<HTMLElement>()
let lineChart: echarts.ECharts | null = null

async function fetchStats() {
  loading.value = true
  try {
    data.value = await statsApi.year(year.value)
    await nextTick()
    renderChart()
  } finally {
    loading.value = false
  }
}

function renderChart() {
  if (!lineRef.value) return
  lineChart = lineChart ?? echarts.init(lineRef.value)
  const months = data.value?.monthlyStats ?? []
  lineChart.setOption({
    tooltip: { trigger: 'axis' },
    legend: { data: ['收入', '支出'] },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: { type: 'category', data: months.map((m: any) => m.yearMonth) },
    yAxis: { type: 'value' },
    series: [
      { name: '收入', type: 'line', smooth: true, data: months.map((m: any) => Number(m.income)), itemStyle: { color: '#67c23a' }, areaStyle: { opacity: 0.1 } },
      { name: '支出', type: 'line', smooth: true, data: months.map((m: any) => Number(m.expense)), itemStyle: { color: '#f56c6c' }, areaStyle: { opacity: 0.1 } },
    ],
  })
}

onMounted(fetchStats)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">年度统计</h2>
      <div style="display:flex;gap:8px;align-items:center">
        <el-input-number v-model="year" :min="2000" :max="2099" @change="fetchStats" controls-position="right" />
        <span>年</span>
      </div>
    </div>
    <div v-loading="loading">
      <el-card shadow="never" header="全年收支趋势" style="margin-bottom:16px">
        <div ref="lineRef" style="height:320px" />
      </el-card>
      <el-card shadow="never" header="全年支出 Top 分类">
        <el-empty v-if="!data?.topCategories?.length" description="暂无数据" />
        <div v-for="(item, i) in data?.topCategories" :key="item.categoryId" class="rank-item">
          <span class="rank-no" :style="{background: i<3?'#f56c6c':'#909399'}">{{ i+1 }}</span>
          <span class="rank-name">{{ item.categoryName }}</span>
          <el-progress :percentage="Math.round(item.percent)" :color="item.color||'#409eff'" style="flex:1;margin:0 12px" />
          <span class="rank-amount">¥{{ Number(item.amount).toFixed(2) }}</span>
        </div>
      </el-card>
    </div>
  </div>
</template>

<style scoped>
.rank-item { display:flex; align-items:center; padding:10px 0; border-bottom:1px solid #f5f5f5; }
.rank-item:last-child { border-bottom:none; }
.rank-no { width:24px; height:24px; border-radius:50%; color:#fff; display:flex; align-items:center; justify-content:center; font-size:12px; font-weight:700; margin-right:12px; flex-shrink:0; }
.rank-name { min-width:60px; font-size:14px; }
.rank-amount { min-width:90px; text-align:right; font-size:13px; color:#666; }
</style>
