<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { recurringApi } from '@/api/recurring'
import { categoriesApi, accountsApi } from '@/api/index'
import { ElMessage, ElMessageBox } from 'element-plus'

const rules = ref<any[]>([])
const categories = ref<any[]>([])
const accounts = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = ref({ categoryId: undefined as number | undefined, accountId: undefined as number | undefined, amount: '', note: '', dayOfMonth: 1 })

const days = Array.from({ length: 28 }, (_, i) => ({ label: `每月 ${i+1} 号`, value: i+1 }))

async function fetchAll() {
  loading.value = true
  try {
    const [r, c, a] = await Promise.all([recurringApi.list(), categoriesApi.list(), accountsApi.list()])
    rules.value = r
    categories.value = c
    accounts.value = a
  } finally { loading.value = false }
}

function openCreate() {
  editingId.value = null
  form.value = { categoryId: undefined, accountId: undefined, amount: '', note: '', dayOfMonth: 1 }
  dialogVisible.value = true
}

function openEdit(rule: any) {
  editingId.value = rule.id
  form.value = { categoryId: rule.categoryId, accountId: rule.accountId, amount: String(rule.amount), note: rule.note || '', dayOfMonth: rule.dayOfMonth }
  dialogVisible.value = true
}

async function handleSave() {
  if (!form.value.categoryId || !form.value.accountId || !form.value.amount)
    return ElMessage.warning('请填写完整信息')
  const data = { ...form.value, amount: Number(form.value.amount) }
  if (editingId.value) {
    await recurringApi.update(editingId.value, data)
    ElMessage.success('更新成功')
  } else {
    await recurringApi.create(data)
    ElMessage.success('创建成功')
  }
  dialogVisible.value = false
  fetchAll()
}

async function handleDelete(id: number) {
  await ElMessageBox.confirm('确认删除该周期规则？', '提示', { type: 'warning' })
  await recurringApi.remove(id)
  ElMessage.success('删除成功')
  fetchAll()
}

async function handleToggle(rule: any) {
  await recurringApi.toggle(rule.id)
  rule.isActive = !rule.isActive
}

onMounted(fetchAll)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">周期账单</h2>
      <el-button type="primary" @click="openCreate">+ 新建规则</el-button>
    </div>

    <el-alert type="info" :closable="false" style="margin-bottom:16px">
      周期账单会在每月指定日期自动生成一笔支出记录，适合房租、话费等固定支出。
    </el-alert>

    <el-card shadow="never" v-loading="loading">
      <el-empty v-if="!rules.length" description="暂无周期规则，点击右上角新建" />
      <div v-for="rule in rules" :key="rule.id" class="rule-item">
        <div class="rule-left">
          <el-tag :type="rule.isActive ? 'success' : 'info'" size="small">{{ rule.isActive ? '启用' : '停用' }}</el-tag>
          <div class="rule-info">
            <span class="rule-name">{{ rule.categoryName }}</span>
            <span class="rule-sub">{{ rule.accountName }} · 每月{{ rule.dayOfMonth }}号</span>
          </div>
        </div>
        <div class="rule-right">
          <span class="rule-amount">-¥{{ Number(rule.amount).toFixed(2) }}</span>
          <el-switch :model-value="rule.isActive" @change="handleToggle(rule)" style="margin:0 8px" />
          <el-button link size="small" @click="openEdit(rule)">编辑</el-button>
          <el-button link size="small" type="danger" @click="handleDelete(rule.id)">删除</el-button>
        </div>
      </div>
    </el-card>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑规则' : '新建周期规则'" width="420px">
      <el-form label-width="90px">
        <el-form-item label="支出分类">
          <el-select v-model="form.categoryId" placeholder="选择分类" style="width:100%">
            <el-option v-for="c in categories.filter((c:any)=>c.type===0)" :key="c.id" :label="c.name" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="扣款账户">
          <el-select v-model="form.accountId" placeholder="选择账户" style="width:100%">
            <el-option v-for="a in accounts" :key="a.id" :label="a.name" :value="a.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="金额">
          <el-input v-model="form.amount" type="number" placeholder="每次扣款金额" />
        </el-form-item>
        <el-form-item label="每月几号">
          <el-select v-model="form.dayOfMonth" style="width:100%">
            <el-option v-for="d in days" :key="d.value" :label="d.label" :value="d.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.note" placeholder="如：房租、话费（选填）" />
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
.rule-item { display:flex; justify-content:space-between; align-items:center; padding:14px 0; border-bottom:1px solid #f0f0f0; }
.rule-item:last-child { border-bottom:none; }
.rule-left { display:flex; align-items:center; gap:12px; }
.rule-info { display:flex; flex-direction:column; }
.rule-name { font-size:14px; font-weight:600; }
.rule-sub { font-size:12px; color:#999; margin-top:2px; }
.rule-right { display:flex; align-items:center; gap:4px; }
.rule-amount { font-size:16px; font-weight:700; color:#f56c6c; min-width:80px; text-align:right; }
</style>
