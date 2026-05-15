<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { recordsApi } from '@/api/records'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Upload } from '@element-plus/icons-vue'

const router = useRouter()
const loading = ref(false)
const exporting = ref(false)
const importing = ref(false)
const importDialogVisible = ref(false)
const importResult = ref<{ message: string; errors?: string[] } | null>(null)
const records = ref<any[]>([])
const total = ref(0)
const query = ref({
  page: 1,
  pageSize: 20,
  type: undefined as number | undefined,
  keyword: '',
  startDate: undefined as string | undefined,
  endDate: undefined as string | undefined,
})
const dateRange = ref<[string, string] | null>(null)

async function fetchRecords() {
  loading.value = true
  try {
    const res = await recordsApi.list(query.value)
    records.value = res.items ?? []
    total.value = res.total ?? 0
  } finally {
    loading.value = false
  }
}

function onDateRangeChange(val: [string, string] | null) {
  query.value.startDate = val?.[0] ?? undefined
  query.value.endDate = val?.[1] ?? undefined
  query.value.page = 1
  fetchRecords()
}

async function handleDelete(id: number) {
  await ElMessageBox.confirm('确认删除该条账单？', '提示', { type: 'warning' })
  await recordsApi.remove(id)
  ElMessage.success('删除成功')
  fetchRecords()
}

async function handleExport() {
  exporting.value = true
  try {
    const blob = await recordsApi.exportFile(query.value)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `账单_${new Date().toISOString().slice(0, 10)}.xlsx`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('导出成功')
  } finally {
    exporting.value = false
  }
}

function downloadTemplate() {
  const csv = '日期,类型,金额,分类,账户,备注,标签\n2026-05-15,支出,45.50,餐饮,微信钱包,午饭,餐饮\n2026-05-15,收入,8000,工资,建行储蓄卡,五月工资,工资\n'
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = '账单导入模板.csv'
  a.click()
  URL.revokeObjectURL(url)
}

async function handleImport(file: File) {
  importing.value = true
  importResult.value = null
  try {
    const res = await recordsApi.importFile(file)
    importResult.value = res
    ElMessage.success(res.message)
    importDialogVisible.value = false
    fetchRecords()
  } catch (e: any) {
    ElMessage.error(e?.message || '导入失败')
  } finally {
    importing.value = false
  }
  return false // 阻止 el-upload 自动上传
}

onMounted(fetchRecords)

// 按日期分组
const groupedRecords = computed(() => {
  const groups: { label: string; items: any[]; expense: number; income: number }[] = []
  const map = new Map<string, typeof groups[0]>()
  const today = new Date()
  const yesterday = new Date(today); yesterday.setDate(today.getDate() - 1)

  for (const r of records.value) {
    const d = new Date(r.occurredAt)
    let label: string
    if (d.toDateString() === today.toDateString()) label = '今天'
    else if (d.toDateString() === yesterday.toDateString()) label = '昨天'
    else label = d.toLocaleDateString('zh-CN', { month: 'long', day: 'numeric' })

    if (!map.has(label)) {
      const g = { label, items: [], expense: 0, income: 0 }
      map.set(label, g)
      groups.push(g)
    }
    const g = map.get(label)!
    g.items.push(r)
    if (r.type === 0) g.expense += Number(r.amount)
    if (r.type === 1) g.income += Number(r.amount)
  }
  return groups
})
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">账单列表</h2>
      <div style="display:flex;gap:8px">
        <el-button @click="importDialogVisible = true">导入</el-button>
        <el-button :loading="exporting" @click="handleExport">导出 Excel</el-button>
        <el-button type="primary" @click="router.push('/records/add')">+ 新增账单</el-button>
      </div>
    </div>

    <el-card shadow="never" style="margin-bottom:16px">
      <el-row :gutter="12" align="middle">
        <el-col :span="5">
          <el-select v-model="query.type" placeholder="全部类型" clearable @change="() => { query.page=1; fetchRecords() }">
            <el-option label="支出" :value="0" />
            <el-option label="收入" :value="1" />
            <el-option label="转账" :value="2" />
          </el-select>
        </el-col>
        <el-col :span="8">
          <el-date-picker
            v-model="dateRange"
            type="daterange"
            range-separator="至"
            start-placeholder="开始日期"
            end-placeholder="结束日期"
            value-format="YYYY-MM-DD"
            style="width:100%"
            @change="onDateRangeChange"
          />
        </el-col>
        <el-col :span="6">
          <el-input v-model="query.keyword" placeholder="搜索备注/标签" clearable @keyup.enter="() => { query.page=1; fetchRecords() }" />
        </el-col>
        <el-col :span="3">
          <el-button @click="() => { query.page=1; fetchRecords() }">搜索</el-button>
        </el-col>
      </el-row>
    </el-card>

    <el-card shadow="never" v-loading="loading">
      <el-empty v-if="!records.length" description="暂无账单记录" />
      <template v-else>
        <div v-for="group in groupedRecords" :key="group.label">
          <!-- 日期分组标题 -->
          <div class="date-group-header">
            <span class="date-label">{{ group.label }}</span>
            <span class="date-summary">
              <span v-if="group.income > 0" class="amount-income">+¥{{ group.income.toFixed(2) }}</span>
              <span v-if="group.expense > 0" class="amount-expense" style="margin-left:8px">-¥{{ group.expense.toFixed(2) }}</span>
            </span>
          </div>
          <div v-for="r in group.items" :key="r.id" class="record-item">
            <div class="record-left">
              <span class="category-tag" :style="{ background: r.categoryColor || '#409eff' }">
                {{ r.categoryIcon }} {{ r.categoryName }}
              </span>
              <span class="record-note">{{ r.note || r.tags || '-' }}</span>
            </div>
            <div class="record-right">
              <span :class="r.type === 1 ? 'amount-income' : 'amount-expense'">
                {{ r.type === 1 ? '+' : '-' }}¥{{ Number(r.amount).toFixed(2) }}
              </span>
              <el-button link size="small" @click="router.push(`/records/${r.id}/edit`)">编辑</el-button>
              <el-button link size="small" type="danger" @click="handleDelete(r.id)">删除</el-button>
            </div>
          </div>
        </div>
        <el-pagination
          style="margin-top:16px;justify-content:flex-end;display:flex"
          v-model:current-page="query.page"
          :page-size="query.pageSize"
          :total="total"
          layout="total, prev, pager, next"
          @current-change="fetchRecords"
        />
      </template>
    </el-card>

    <!-- 导入对话框 -->
    <el-dialog v-model="importDialogVisible" title="导入账单" width="480px">
      <div style="margin-bottom:16px">
        <el-alert type="info" :closable="false" style="margin-bottom:12px">
          <template #title>
            文件格式要求：CSV 或 Excel，列顺序为<b>日期、类型、金额、分类、账户、备注、标签</b>。
            类型填「支出」「收入」「转账」，分类和账户若不存在将使用默认值。
          </template>
        </el-alert>
        <el-button size="small" @click="downloadTemplate">下载 CSV 模板</el-button>
      </div>

      <el-upload
        drag
        :auto-upload="false"
        accept=".csv,.xlsx,.xls"
        :limit="1"
        :on-change="(file: any) => handleImport(file.raw)"
        :show-file-list="true"
      >
        <el-icon style="font-size:40px;color:#c0c4cc"><Upload /></el-icon>
        <div style="margin-top:8px;color:#606266">将文件拖到此处，或 <em style="color:#409eff">点击上传</em></div>
        <template #tip>
          <div style="color:#909399;font-size:12px;margin-top:4px">支持 .csv / .xlsx / .xls，单次最多导入 1000 行</div>
        </template>
      </el-upload>

      <div v-if="importing" style="text-align:center;margin-top:12px">
        <el-text type="info">正在导入，请稍候...</el-text>
      </div>
      <div v-if="importResult?.errors?.length" style="margin-top:12px">
        <el-alert type="warning" title="部分行导入失败" :closable="false">
          <div v-for="e in importResult.errors" :key="e" style="font-size:12px">{{ e }}</div>
        </el-alert>
      </div>

      <template #footer>
        <el-button @click="importDialogVisible = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.date-group-header { display:flex; justify-content:space-between; align-items:center; padding:10px 0 6px; border-bottom:2px solid #f0f0f0; margin-top:8px; }
.date-group-header:first-child { margin-top:0; }
.date-label { font-size:13px; font-weight:600; color:#333; }
.date-summary { font-size:12px; }
.record-item { display:flex; justify-content:space-between; align-items:center; padding:10px 0; border-bottom:1px solid #f8f8f8; }
.record-item:last-child { border-bottom:none; }
.record-left { display:flex; align-items:center; gap:12px; }
.category-tag { padding:2px 8px; border-radius:12px; font-size:12px; color:#fff; white-space:nowrap; }
.record-note { font-size:13px; color:#666; }
.record-right { display:flex; align-items:center; gap:8px; }
.amount-income { color:#67c23a; font-weight:700; font-size:15px; }
.amount-expense { color:#f56c6c; font-weight:700; font-size:15px; }
</style>
