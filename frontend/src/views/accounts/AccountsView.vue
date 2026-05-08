<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { accountsApi } from '@/api/index'
import { ElMessage, ElMessageBox } from 'element-plus'

const accounts = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = ref({ name: '', type: 0, balance: '', note: '' })
const typeLabels: Record<number, string> = { 0:'现金', 1:'银行卡', 2:'信用卡', 3:'支付宝', 4:'微信', 5:'其他' }

async function fetchAccounts() {
  loading.value = true
  try { accounts.value = await accountsApi.list() }
  finally { loading.value = false }
}
function openCreate() {
  editingId.value = null
  form.value = { name: '', type: 0, balance: '', note: '' }
  dialogVisible.value = true
}
function openEdit(acc: any) {
  editingId.value = acc.id
  form.value = { name: acc.name, type: acc.type, balance: String(acc.balance), note: acc.note || '' }
  dialogVisible.value = true
}
async function handleSave() {
  if (!form.value.name) return ElMessage.warning('请填写账户名称')
  const data = { ...form.value, balance: Number(form.value.balance) }
  if (editingId.value) {
    await accountsApi.update(editingId.value, data)
    ElMessage.success('更新成功')
  } else {
    await accountsApi.create(data)
    ElMessage.success('创建成功')
  }
  dialogVisible.value = false
  fetchAccounts()
}
async function handleDelete(id: number) {
  await ElMessageBox.confirm('确认删除该账户？', '提示', { type: 'warning' })
  await accountsApi.remove(id)
  ElMessage.success('删除成功')
  fetchAccounts()
}
onMounted(fetchAccounts)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">账户管理</h2>
      <el-button type="primary" @click="openCreate">+ 新增账户</el-button>
    </div>
    <el-row :gutter="16" v-loading="loading">
      <el-empty v-if="!accounts.length" description="暂无账户，请先创建" style="width:100%" />
      <el-col :span="8" v-for="acc in accounts" :key="acc.id" style="margin-bottom:16px">
        <el-card shadow="hover">
          <div style="display:flex;justify-content:space-between;align-items:flex-start">
            <div>
              <div style="font-size:16px;font-weight:600">{{ acc.name }}</div>
              <div style="font-size:12px;color:#999;margin-top:4px">{{ typeLabels[acc.type] || '其他' }}</div>
            </div>
            <div style="font-size:20px;font-weight:700;color:#409eff">¥{{ acc.balance?.toFixed(2) }}</div>
          </div>
          <div v-if="acc.note" style="font-size:12px;color:#aaa;margin-top:8px">{{ acc.note }}</div>
          <div style="margin-top:12px;display:flex;gap:8px;justify-content:flex-end">
            <el-button size="small" @click="openEdit(acc)">编辑</el-button>
            <el-button size="small" type="danger" @click="handleDelete(acc.id)">删除</el-button>
          </div>
        </el-card>
      </el-col>
    </el-row>
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑账户' : '新增账户'" width="400px">
      <el-form label-width="80px">
        <el-form-item label="名称"><el-input v-model="form.name" placeholder="账户名称" /></el-form-item>
        <el-form-item label="类型">
          <el-select v-model="form.type" style="width:100%">
            <el-option v-for="(label, val) in typeLabels" :key="val" :label="label" :value="+val" />
          </el-select>
        </el-form-item>
        <el-form-item label="余额"><el-input v-model="form.balance" type="number" placeholder="初始余额" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.note" placeholder="备注（选填）" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible=false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>
