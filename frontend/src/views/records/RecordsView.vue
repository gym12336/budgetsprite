<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { recordsApi } from '@/api/records'
import { ElMessage, ElMessageBox } from 'element-plus'

const router = useRouter()
const loading = ref(false)
const records = ref<any[]>([])
const total = ref(0)
const query = ref({ page: 1, pageSize: 20, type: undefined as number | undefined, keyword: '' })

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

async function handleDelete(id: number) {
  await ElMessageBox.confirm('确认删除该条账单？', '提示', { type: 'warning' })
  await recordsApi.remove(id)
  ElMessage.success('删除成功')
  fetchRecords()
}

onMounted(fetchRecords)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">账单列表</h2>
      <el-button type="primary" @click="router.push('/records/add')">+ 新增账单</el-button>
    </div>

    <el-card shadow="never" style="margin-bottom:16px">
      <el-row :gutter="12" align="middle">
        <el-col :span="6">
          <el-select v-model="query.type" placeholder="全部类型" clearable @change="fetchRecords">
            <el-option label="支出" :value="0" />
            <el-option label="收入" :value="1" />
            <el-option label="转账" :value="2" />
          </el-select>
        </el-col>
        <el-col :span="8">
          <el-input v-model="query.keyword" placeholder="搜索备注/标签" clearable @keyup.enter="fetchRecords" />
        </el-col>
        <el-col :span="4">
          <el-button @click="fetchRecords">搜索</el-button>
        </el-col>
      </el-row>
    </el-card>

    <el-card shadow="never" v-loading="loading">
      <el-empty v-if="!records.length" description="暂无账单记录" />
      <template v-else>
        <div v-for="r in records" :key="r.id" class="record-item">
          <div class="record-left">
            <span class="category-tag" :style="{ background: r.categoryColor || '#409eff' }">
              {{ r.categoryIcon }} {{ r.categoryName }}
            </span>
            <span class="record-note">{{ r.note || r.tags || '-' }}</span>
            <span class="record-date">{{ new Date(r.occurredAt).toLocaleDateString('zh-CN') }}</span>
          </div>
          <div class="record-right">
            <span :class="r.type === 1 ? 'amount-income' : 'amount-expense'">
              {{ r.type === 1 ? '+' : '-' }}¥{{ r.amount?.toFixed(2) }}
            </span>
            <el-button link size="small" @click="router.push(`/records/${r.id}/edit`)">编辑</el-button>
            <el-button link size="small" type="danger" @click="handleDelete(r.id)">删除</el-button>
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
  </div>
</template>

<style scoped>
.record-item { display:flex; justify-content:space-between; align-items:center; padding:12px 0; border-bottom:1px solid #f0f0f0; }
.record-item:last-child { border-bottom:none; }
.record-left { display:flex; align-items:center; gap:12px; }
.category-tag { padding:2px 8px; border-radius:12px; font-size:12px; color:#fff; }
.record-note { font-size:13px; color:#666; }
.record-date { font-size:12px; color:#aaa; }
.record-right { display:flex; align-items:center; gap:8px; }
.amount-income { color:#67c23a; font-weight:700; font-size:15px; }
.amount-expense { color:#f56c6c; font-weight:700; font-size:15px; }
</style>
