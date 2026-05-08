import { defineStore } from 'pinia'
import { ref } from 'vue'
import { recordsApi } from '@/api/records'

export const useRecordStore = defineStore('record', () => {
  const records = ref<any[]>([])
  const total = ref(0)
  const loading = ref(false)

  async function fetchRecords(params: object) {
    loading.value = true
    try {
      const res = await recordsApi.list(params)
      records.value = res.items
      total.value = res.total
    } finally {
      loading.value = false
    }
  }

  async function createRecord(data: object) {
    await recordsApi.create(data)
  }

  async function updateRecord(id: number, data: object) {
    await recordsApi.update(id, data)
  }

  async function removeRecord(id: number) {
    await recordsApi.remove(id)
  }

  return { records, total, loading, fetchRecords, createRecord, updateRecord, removeRecord }
})
