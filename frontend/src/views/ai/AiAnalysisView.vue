<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { aiApi } from '@/api/ai'
import { statsApi } from '@/api/stats'
import * as echarts from 'echarts'

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1)
const activeTab = ref('single')
const trendMonths = ref(3)
const data = ref<any>(null)
const loading = ref(false)
const displayedSummary = ref('')
let typingTimer: ReturnType<typeof setInterval> | null = null

// ECharts refs
const pieRef = ref<HTMLElement>()
const lineRef = ref<HTMLElement>()
let pieChart: echarts.ECharts | null = null
let lineChart: echarts.ECharts | null = null

const levelConfig: Record<string, { border: string; bg: string; tag: string }> = {
  good:    { border: '#67c23a', bg: '#f0f9eb', tag: 'success' },
  warning: { border: '#e6a23c', bg: '#fdf6ec', tag: 'warning' },
  danger:  { border: '#f56c6c', bg: '#fef0f0', tag: 'danger'  },
  info:    { border: '#409eff', bg: '#ecf5ff', tag: 'info'    },
}

async function fetchAnalysis() {
  loading.value = true
  displayedSummary.value = ''
  data.value = null
  disposeCharts()
  if (typingTimer) clearInterval(typingTimer)
  try {
    if (activeTab.value === 'single') {
      data.value = await aiApi.analysis(year.value, month.value)
      await nextTick()
      await renderCharts()
    } else {
      data.value = await aiApi.trendAnalysis(trendMonths.value)
      await nextTick()
      await renderTrendCharts()
    }
    startTyping(data.value.summary)
  } finally {
    loading.value = false
  }
}

function startTyping(text: string) {
  let i = 0
  displayedSummary.value = ''
  typingTimer = setInterval(() => {
    if (i < text.length) displayedSummary.value += text[i++]
    else clearInterval(typingTimer!)
  }, 55)
}

async function renderCharts() {
  // 饼图：分类占比
  try {
    const statsData = await statsApi.month(year.value, month.value)
    await nextTick()
    if (pieRef.value && statsData.categoryStats?.length) {
      pieChart = echarts.init(pieRef.value)
      pieChart.setOption({
        tooltip: { trigger: 'item', formatter: '{b}: ¥{c} ({d}%)' },
        legend: { bottom: 0, type: 'scroll', textStyle: { fontSize: 11 } },
        series: [{
          type: 'pie', radius: ['40%', '68%'],
          data: statsData.categoryStats.map((c: any) => ({
            name: c.categoryName, value: Number(c.amount),
            itemStyle: { color: c.color || undefined }
          })),
          label: { show: false },
          emphasis: { label: { show: true, fontSize: 13, fontWeight: 'bold' } }
        }]
      })
    }
  } catch { /* 图表加载失败不影响主功能 */ }
}

async function renderTrendCharts() {
  // 折线图：近几月支出趋势
  try {
    const promises = []
    for (let i = trendMonths.value - 1; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1)
      promises.push(statsApi.month(d.getFullYear(), d.getMonth() + 1))
    }
    const results = await Promise.all(promises)
    await nextTick()
    if (lineRef.value) {
      lineChart = echarts.init(lineRef.value)
      lineChart.setOption({
        tooltip: { trigger: 'axis' },
        legend: { data: ['支出', '收入'] },
        grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
        xAxis: { type: 'category', data: results.map((_: any, i: number) => {
          const d = new Date(now.getFullYear(), now.getMonth() - (trendMonths.value - 1 - i), 1)
          return `${d.getMonth()+1}月`
        })},
        yAxis: { type: 'value' },
        series: [
          { name: '支出', type: 'line', smooth: true, data: results.map((r: any) => Number(r.totalExpense)), itemStyle: { color: '#f56c6c' }, areaStyle: { opacity: 0.1 } },
          { name: '收入', type: 'line', smooth: true, data: results.map((r: any) => Number(r.totalIncome)), itemStyle: { color: '#67c23a' }, areaStyle: { opacity: 0.1 } },
        ]
      })
    }
  } catch { /* 图表加载失败不影响主功能 */ }
}

function disposeCharts() {
  pieChart?.dispose(); pieChart = null
  lineChart?.dispose(); lineChart = null
}

onMounted(fetchAnalysis)
onUnmounted(() => { disposeCharts(); if (typingTimer) clearInterval(typingTimer) })
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">🤖 AI 智能分析</h2>
      <div style="display:flex;gap:8px;align-items:center">
        <template v-if="activeTab === 'single'">
          <el-date-picker
            :model-value="`${year}-${String(month).padStart(2,'0')}`"
            type="month" value-format="YYYY-MM" style="width:140px"
            @update:model-value="(v:string) => { const [y,m]=v.split('-'); year=+y; month=+m; fetchAnalysis() }"
          />
        </template>
        <template v-else>
          <el-radio-group v-model="trendMonths" @change="fetchAnalysis">
            <el-radio-button :value="3">近3个月</el-radio-button>
            <el-radio-button :value="6">近6个月</el-radio-button>
          </el-radio-group>
        </template>
        <el-button :loading="loading" @click="fetchAnalysis">重新分析</el-button>
      </div>
    </div>

    <!-- Tab 切换 -->
    <el-tabs v-model="activeTab" @tab-change="fetchAnalysis" style="margin-bottom:16px">
      <el-tab-pane label="单月分析" name="single" />
      <el-tab-pane label="趋势分析" name="trend" />
    </el-tabs>

    <!-- 加载中 -->
    <div v-if="loading" style="text-align:center;padding:60px 0">
      <div style="font-size:48px;margin-bottom:16px">🤖</div>
      <el-text type="info">AI 正在{{ activeTab === 'single' ? '分析本月财务数据' : '分析多月趋势' }}...</el-text>
      <div style="margin-top:12px">
        <el-progress :percentage="100" status="striped" striped-flow :duration="1" style="max-width:300px;margin:0 auto" />
      </div>
    </div>

    <template v-else-if="data">
      <!-- AI 总结气泡 -->
      <el-card shadow="never" style="margin-bottom:20px;border:1px solid #409eff;background:#ecf5ff">
        <div style="display:flex;align-items:flex-start;gap:16px">
          <div style="font-size:40px;flex-shrink:0">🤖</div>
          <div>
            <div style="font-size:12px;color:#409eff;margin-bottom:6px;font-weight:600">
              AI 助手 · {{ activeTab === 'single' ? `${year}年${month}月财务分析` : `最近${trendMonths}个月趋势分析` }}
            </div>
            <div style="font-size:16px;font-weight:600;color:#303133;min-height:24px">
              {{ displayedSummary }}<span v-if="displayedSummary.length < data.summary.length" class="cursor">|</span>
            </div>
          </div>
        </div>
      </el-card>

      <!-- 洞察卡片 -->
      <el-row :gutter="16" style="margin-bottom:20px">
        <el-col :span="12" v-for="insight in data.insights" :key="insight.type + insight.title" style="margin-bottom:16px">
          <el-card shadow="hover" :style="{ border: `1px solid ${levelConfig[insight.level]?.border || '#409eff'}`, background: levelConfig[insight.level]?.bg || '#ecf5ff', height: '100%' }">
            <div style="display:flex;align-items:center;margin-bottom:8px;gap:8px">
              <span style="font-size:22px">{{ insight.icon }}</span>
              <span style="font-weight:600;font-size:14px">{{ insight.title }}</span>
              <el-tag :type="levelConfig[insight.level]?.tag || 'info'" size="small" style="margin-left:auto">
                {{ { good:'良好', warning:'注意', danger:'警告', info:'提示' }[insight.level] || insight.level }}
              </el-tag>
            </div>
            <div style="font-size:13px;color:#606266;line-height:1.6">{{ insight.content }}</div>
          </el-card>
        </el-col>
      </el-row>

      <!-- ECharts 图表 -->
      <el-row :gutter="16" style="margin-bottom:16px">
        <el-col :span="activeTab === 'single' ? 10 : 24">
          <el-card shadow="never" :header="activeTab === 'single' ? '本月支出分类占比' : `近${trendMonths}个月收支趋势`">
            <div v-if="activeTab === 'single'" ref="pieRef" style="height:260px" />
            <div v-else ref="lineRef" style="height:260px" />
          </el-card>
        </el-col>
        <el-col v-if="activeTab === 'single'" :span="14">
          <el-card shadow="never" header="数据说明">
            <div style="font-size:13px;color:#606266;line-height:1.8">
              <div>📅 分析周期：{{ year }}年{{ month }}月</div>
              <div>🔢 洞察条数：{{ data.insights?.length }} 条</div>
              <div>🤖 AI 模型：DeepSeek Chat</div>
              <div style="margin-top:8px;padding:8px;background:#f5f7fa;border-radius:4px;font-size:12px;color:#999">
                分析结果基于您的真实账单数据，由 AI 生成，仅供参考。数据缓存5分钟，点「重新分析」获取最新结果。
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </template>
  </div>
</template>

<style scoped>
@keyframes blink { 0%,100%{opacity:1} 50%{opacity:0} }
.cursor { animation: blink 1s infinite; color: #409eff; }
</style>
