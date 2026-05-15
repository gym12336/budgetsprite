<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { budgetsApi } from '@/api/index'
import { ElMessage, ElMessageBox } from 'element-plus'

const now = new Date()
const yearMonth = ref(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`)
const budgets = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const form = ref({ yearMonth: yearMonth.value, amount: '' })

async function fetchBudgets() {
  loading.value = true
  try { budgets.value = await budgetsApi.list(yearMonth.value) }
  finally { loading.value = false }
}

async function handleSave() {
  if (!form.value.amount) return ElMessage.warning('请填写预算金额')
  await budgetsApi.upsert({ ...form.value, amount: Number(form.value.amount) })
  ElMessage.success('保存成功')
  dialogVisible.value = false
  fetchBudgets()
}

async function handleDelete(id: number) {
  await ElMessageBox.confirm('确认删除该预算？', '提示', { type: 'warning' })
  await budgetsApi.remove(id)
  ElMessage.success('删除成功')
  fetchBudgets()
}

onMounted(fetchBudgets)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">预算管理</h2>
      <div style="display:flex;gap:8px;align-items:center">
        <el-date-picker v-model="yearMonth" type="month" value-format="YYYY-MM" @change="fetchBudgets" />
        <el-button type="primary" @click="dialogVisible=true">+ 新增预算</el-button>
      </div>
    </div>
    <el-card shadow="never" v-loading="loading">
      <el-empty v-if="!budgets.length" description="本月暂无预算" />
      <div
        v-for="b in budgets" :key="b.id" class="budget-item"
        :class="{ 'over-budget': b.remaining < 0, 'warn-budget': b.remaining >= 0 && b.used / b.amount >= 0.8 }"
      >
        <div class="budget-header">
          <span class="budget-name">{{ b.categoryName || '总预算' }}</span>
          <el-tag v-if="b.remaining < 0" type="danger" size="small" style="margin-right:8px">⚠️ 超支 ¥{{ Math.abs(b.remaining).toFixed(0) }}</el-tag>
          <el-tag v-else-if="b.used / b.amount >= 0.8" type="warning" size="small" style="margin-right:8px">注意：已用 {{ Math.round(b.used/b.amount*100) }}%</el-tag>
          <span class="budget-amount">{{ Number(b.used).toFixed(2) }} / {{ Number(b.amount).toFixed(2) }} 元</span>
          <el-button link type="danger" size="small" @click="handleDelete(b.id)">删除</el-button>
        </div>
        <el-progress
          :percentage="Math.min(100, Math.round((b.used / b.amount) * 100))"
          :status="b.remaining < 0 ? 'exception' : b.used / b.amount >= 0.8 ? 'warning' : 'success'"
          :stroke-width="10"
        />
        <div style="margin-top:6px;font-size:13px" :style="{color: b.remaining>=0?'#67c23a':'#f56c6c', fontWeight: b.remaining<0?'700':'400'}">
          {{ b.remaining >= 0 ? `剩余：¥${Number(b.remaining).toFixed(2)}` : `超支：¥${Math.abs(b.remaining).toFixed(2)}` }}
        </div>
      </div>
    </el-card>
    <el-dialog v-model="dialogVisible" title="新增预算" width="400px">
      <el-form label-width="80px">
        <el-form-item label="月份">
          <el-date-picker v-model="form.yearMonth" type="month" value-format="YYYY-MM" style="width:100%" />
        </el-form-item>
        <el-form-item label="金额">
          <el-input v-model="form.amount" type="number" placeholder="预算金额" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible=false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.budget-item { margin-bottom:20px; padding:16px; border-bottom:1px solid #f0f0f0; border-radius:8px; transition: border 0.2s; }
.budget-item:last-child { border-bottom:none; }
.budget-item.over-budget { border:1px solid #f56c6c; background:#fff5f5; }
.budget-item.warn-budget { border:1px solid #e6a23c; background:#fffbf0; }
.budget-header { display:flex; align-items:center; margin-bottom:8px; }
.budget-name { font-weight:600; flex:1; }
.budget-amount { font-size:13px; color:#888; margin-right:12px; }
</style>
