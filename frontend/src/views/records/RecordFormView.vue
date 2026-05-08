<script setup lang="ts">
import { reactive, ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { recordsApi } from '@/api/records'
import { categoriesApi, accountsApi } from '@/api/index'
import { ElMessage } from 'element-plus'

const router = useRouter()
const route = useRoute()
const isEdit = computed(() => !!route.params.id)
const loading = ref(false)
const categories = ref<any[]>([])
const accounts = ref<any[]>([])

const form = reactive({
  type: 0,
  amount: '',
  categoryId: undefined as number | undefined,
  accountId: undefined as number | undefined,
  occurredAt: new Date().toISOString().slice(0, 10),
  note: '',
  tags: '',
})

onMounted(async () => {
  const [cats, accs] = await Promise.all([categoriesApi.list(), accountsApi.list()])
  categories.value = cats
  accounts.value = accs
})

async function handleSubmit() {
  if (!form.amount || !form.categoryId || !form.accountId)
    return ElMessage.warning('请填写金额、分类和账户')
  loading.value = true
  try {
    const data = { ...form, amount: Number(form.amount) }
    if (isEdit.value) {
      await recordsApi.update(Number(route.params.id), data)
      ElMessage.success('更新成功')
    } else {
      await recordsApi.create(data)
      ElMessage.success('记账成功')
    }
    router.push('/records')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div style="max-width:560px;margin:0 auto">
    <h2>{{ isEdit ? '编辑账单' : '新增账单' }}</h2>
    <el-card shadow="never">
      <el-form label-width="80px" @submit.prevent="handleSubmit">
        <el-form-item label="类型">
          <el-radio-group v-model="form.type">
            <el-radio :value="0">支出</el-radio>
            <el-radio :value="1">收入</el-radio>
            <el-radio :value="2">转账</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="金额">
          <el-input v-model="form.amount" type="number" placeholder="0.00" />
        </el-form-item>
        <el-form-item label="分类">
          <el-select v-model="form.categoryId" placeholder="选择分类" style="width:100%">
            <el-option v-for="c in categories" :key="c.id" :label="c.name" :value="c.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="账户">
          <el-select v-model="form.accountId" placeholder="选择账户" style="width:100%">
            <el-option v-for="a in accounts" :key="a.id" :label="a.name" :value="a.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="日期">
          <el-date-picker v-model="form.occurredAt" type="date" value-format="YYYY-MM-DD" style="width:100%" />
        </el-form-item>
        <el-form-item label="备注">
          <el-input v-model="form.note" placeholder="备注（选填）" />
        </el-form-item>
        <el-form-item label="标签">
          <el-input v-model="form.tags" placeholder="标签，逗号分隔（选填）" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" native-type="submit" :loading="loading">保存</el-button>
          <el-button @click="router.back()">取消</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>
