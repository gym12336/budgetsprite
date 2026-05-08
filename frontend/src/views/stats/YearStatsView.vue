<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { statsApi } from '@/api/stats'

const year = ref(new Date().getFullYear())
const data = ref<any>(null)
const loading = ref(false)

async function fetchStats() {
  loading.value = true
  try { data.value = await statsApi.year(year.value) }
  finally { loading.value = false }
}

onMounted(fetchStats)
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
      <h2 style="margin:0">年度统计</h2>
      <div style="display:flex;gap:8px;align-items:center">
        <el-input-number v-model="year" :min="2000" :max="2099" @change="fetchStats" controls-position="right" />
        <span>年</span>
      </div>
    </div>
    <div v-loading="loading">
      <el-card shadow="never" header="各月收支汇总" style="margin-bottom:16px">
        <el-empty v-if="!data?.monthlyStats?.length" description="暂无数据" />
        <el-table v-else :data="data.monthlyStats" stripe>
          <el-table-column prop="yearMonth" label="月份" width="100" />
          <el-table-column label="收入">
            <template #default="{row}"><span style="color:#67c23a">¥{{ row.income?.toFixed(2) }}</span></template>
          </el-table-column>
          <el-table-column label="支出">
            <template #default="{row}"><span style="color:#f56c6c">¥{{ row.expense?.toFixed(2) }}</span></template>
          </el-table-column>
          <el-table-column label="结余">
            <template #default="{row}">
              <span :style="{color: row.income-row.expense>=0?'#67c23a':'#f56c6c'}">
                ¥{{ (row.income - row.expense)?.toFixed(2) }}
              </span>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
      <el-card shadow="never" header="年度收支趋势（ECharts 折线图待接入）">
        <el-empty description="此处将接入 ECharts 折线图" />
      </el-card>
    </div>
  </div>
</template>
