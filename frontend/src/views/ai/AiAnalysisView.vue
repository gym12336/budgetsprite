<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { aiApi } from '@/api/ai'

const now = new Date()
const year = ref(now.getFullYear())
const month = ref(now.getMonth() + 1)
const data = ref<any>(null)
const loading = ref(false)
const displayedSummary = ref('')
let typingTimer: ReturnType<typeof setInterval> | null = null

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
  if (typingTimer) clearInterval(typingTimer)
  try {
    data.value = await aiApi.analysis(year.value, month.value)
    startTyping(data.value.summary)
  } finally {
    loading.value = false
  }
}

function startTyping(text: string) {
  let i = 0
  displayedSummary.value = ''
  typingTimer = setInterval(() => {
    if (i < text.length) {
      displayedSummary.value += text[i++]
    } else {
      clearInterval(typingTimer!)
    }
  }, 60)
}

onMounted(fetchAnalysis)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">🤖 AI 智能分析</h2>
      <div style="display:flex;gap:8px;align-items:center">
        <el-date-picker
          :model-value="`${year}-${String(month).padStart(2,'0')}`"
          type="month" value-format="YYYY-MM"
          @update:model-value="(v:string) => { const [y,m]=v.split('-'); year=+y; month=+m; fetchAnalysis() }"
        />
        <el-button :loading="loading" @click="fetchAnalysis">重新分析</el-button>
      </div>
    </div>

    <!-- 加载中 -->
    <div v-if="loading" style="text-align:center;padding:60px 0">
      <div style="font-size:48px;margin-bottom:16px">🤖</div>
      <el-text type="info">AI 正在分析您的财务数据...</el-text>
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
            <div style="font-size:12px;color:#409eff;margin-bottom:6px;font-weight:600">AI 助手 · 财务分析</div>
            <div style="font-size:16px;font-weight:600;color:#303133;min-height:24px">
              {{ displayedSummary }}<span v-if="displayedSummary.length < data.summary.length" class="cursor">|</span>
            </div>
          </div>
        </div>
      </el-card>

      <!-- 洞察卡片 -->
      <el-row :gutter="16">
        <el-col :span="12" v-for="insight in data.insights" :key="insight.type" style="margin-bottom:16px">
          <el-card
            shadow="hover"
            :style="{
              border: `1px solid ${levelConfig[insight.level]?.border || '#409eff'}`,
              background: levelConfig[insight.level]?.bg || '#ecf5ff',
              height: '100%'
            }"
          >
            <div style="display:flex;align-items:center;margin-bottom:8px;gap:8px">
              <span style="font-size:22px">{{ insight.icon }}</span>
              <span style="font-weight:600;font-size:14px">{{ insight.title }}</span>
              <el-tag
                :type="levelConfig[insight.level]?.tag || 'info'"
                size="small"
                style="margin-left:auto"
              >{{ { good:'良好', warning:'注意', danger:'警告', info:'提示' }[insight.level] }}</el-tag>
            </div>
            <div style="font-size:13px;color:#606266;line-height:1.6">{{ insight.content }}</div>
          </el-card>
        </el-col>
      </el-row>

      <div style="text-align:center;margin-top:8px;color:#aaa;font-size:12px">
        以上分析基于您 {{ year }}年{{ month }}月 的真实账单数据 · 由 DeepSeek AI 生成
      </div>
    </template>
  </div>
</template>

<style scoped>
@keyframes blink { 0%,100%{opacity:1} 50%{opacity:0} }
.cursor { animation: blink 1s infinite; color: #409eff; }
</style>
