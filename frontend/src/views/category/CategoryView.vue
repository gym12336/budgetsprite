<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { categoriesApi } from '@/api/index'
import { ElMessage, ElMessageBox } from 'element-plus'

const categories = ref<any[]>([])
const loading = ref(false)
const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = ref({ name: '', type: 0, icon: '', color: '#409eff' })

async function fetchCategories() {
  loading.value = true
  try { categories.value = await categoriesApi.list() }
  finally { loading.value = false }
}
function openCreate() {
  editingId.value = null
  form.value = { name: '', type: 0, icon: '', color: '#409eff' }
  dialogVisible.value = true
}
function openEdit(cat: any) {
  editingId.value = cat.id
  form.value = { name: cat.name, type: cat.type, icon: cat.icon || '', color: cat.color || '#409eff' }
  dialogVisible.value = true
}
async function handleSave() {
  if (!form.value.name) return ElMessage.warning('请填写分类名称')
  if (editingId.value) {
    await categoriesApi.update(editingId.value, form.value)
    ElMessage.success('更新成功')
  } else {
    await categoriesApi.create(form.value)
    ElMessage.success('创建成功')
  }
  dialogVisible.value = false
  fetchCategories()
}
async function handleDelete(id: number) {
  await ElMessageBox.confirm('确认删除该分类？', '提示', { type: 'warning' })
  await categoriesApi.remove(id)
  ElMessage.success('删除成功')
  fetchCategories()
}
onMounted(fetchCategories)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">分类管理</h2>
      <el-button type="primary" @click="openCreate">+ 新增分类</el-button>
    </div>
    <el-row :gutter="16">
      <el-col :span="12">
        <el-card shadow="never" header="支出分类">
          <div v-loading="loading">
            <el-empty v-if="!categories.filter(c=>c.type===0).length" description="暂无支出分类" />
            <div v-for="cat in categories.filter(c=>c.type===0)" :key="cat.id" class="cat-item">
              <div class="cat-icon" :style="{background: cat.color||'#eee'}">{{ cat.icon||'📁' }}</div>
              <span style="flex:1;font-size:14px">{{ cat.name }}</span>
              <el-button link size="small" @click="openEdit(cat)">编辑</el-button>
              <el-button link size="small" type="danger" @click="handleDelete(cat.id)">删除</el-button>
            </div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="12">
        <el-card shadow="never" header="收入分类">
          <div v-loading="loading">
            <el-empty v-if="!categories.filter(c=>c.type===1).length" description="暂无收入分类" />
            <div v-for="cat in categories.filter(c=>c.type===1)" :key="cat.id" class="cat-item">
              <div class="cat-icon" :style="{background: cat.color||'#eee'}">{{ cat.icon||'📁' }}</div>
              <span style="flex:1;font-size:14px">{{ cat.name }}</span>
              <el-button link size="small" @click="openEdit(cat)">编辑</el-button>
              <el-button link size="small" type="danger" @click="handleDelete(cat.id)">删除</el-button>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑分类' : '新增分类'" width="400px">
      <el-form label-width="80px">
        <el-form-item label="名称"><el-input v-model="form.name" placeholder="分类名称" /></el-form-item>
        <el-form-item label="类型">
          <el-radio-group v-model="form.type">
            <el-radio :value="0">支出</el-radio>
            <el-radio :value="1">收入</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="图标"><el-input v-model="form.icon" placeholder="emoji 图标，如 🍔" /></el-form-item>
        <el-form-item label="颜色"><el-color-picker v-model="form.color" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="dialogVisible=false">取消</el-button>
        <el-button type="primary" @click="handleSave">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.cat-item { display:flex; align-items:center; padding:8px 0; border-bottom:1px solid #f5f5f5; gap:8px; }
.cat-item:last-child { border-bottom:none; }
.cat-icon { width:32px; height:32px; border-radius:8px; display:flex; align-items:center; justify-content:center; font-size:16px; }
</style>
