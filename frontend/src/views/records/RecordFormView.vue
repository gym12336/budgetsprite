<script setup lang="ts">
import { reactive, ref, onMounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { recordsApi } from '@/api/records'
import { categoriesApi, accountsApi } from '@/api/index'
import { aiApi } from '@/api/ai'
import { ElMessage } from 'element-plus'

const router = useRouter()
const route = useRoute()
const isEdit = computed(() => !!route.params.id)
const loading = ref(false)
const aiLoading = ref(false)
const aiInput = ref('')
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

async function handleAiParse() {
  if (!aiInput.value.trim()) return ElMessage.warning('请输入描述文字')
  aiLoading.value = true
  try {
    const res = await aiApi.parseRecord(aiInput.value)
    if (!res.success) {
      ElMessage.warning(res.errorMessage || '未能识别，请手动填写')
      return
    }
    if (res.amount != null) form.amount = String(res.amount)
    if (res.type != null) form.type = res.type
    if (res.note) form.note = res.note
    if (res.dateHint) form.occurredAt = res.dateHint

    // 模糊匹配分类
    if (res.categoryHint) {
      const hint = res.categoryHint.toLowerCase()
      const allCats: any[] = []
      const flatCats = (list: any[]) => { list.forEach(c => { allCats.push(c); if (c.children) flatCats(c.children) }) }
      flatCats(categories.value)
      const matched = allCats.find(c => c.name.includes(res.categoryHint!) || res.categoryHint!.includes(c.name))
      if (matched) form.categoryId = matched.id
    }

    ElMessage.success('AI 解析成功，请检查并确认')
  } finally {
    aiLoading.value = false
  }
}

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

    <!-- AI 智能填写区域 -->
    <el-card v-if="!isEdit" shadow="never" style="margin-bottom:16px;border:1px solid #409eff;background:#f0f7ff">
      <div style="display:flex;align-items:center;gap:8px;margin-bottom:8px">
        <span style="font-size:16px">🤖</span>
        <span style="font-weight:600;font-size:13px;color:#409eff">AI 智能填写</span>
        <el-tag type="info" size="small">Beta</el-tag>
      </div>
      <div style="display:flex;gap:8px">
        <el-input
          v-model="aiInput"
          placeholder="说一句话，如「今天午饭花了35元」「昨天打车花了15块」"
          @keyup.enter="handleAiParse"
          style="flex:1"
        />
        <el-button type="primary" :loading="aiLoading" @click="handleAiParse">AI 解析</el-button>
      </div>
      <div style="font-size:11px;color:#999;margin-top:6px">AI 将自动填入金额、分类、备注和日期，请确认后保存</div>
    </el-card>

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
