<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/modules/auth'
import { ElMessage } from 'element-plus'

const auth = useAuthStore()
const loading = ref(false)
const profileForm = ref({ nickname: auth.user?.nickname || '' })
const pwdForm = ref({ oldPassword: '', newPassword: '', confirmPassword: '' })

async function handleSaveProfile() {
  loading.value = true
  try { ElMessage.info('功能开发中，请组员接入接口') }
  finally { loading.value = false }
}
async function handleChangePassword() {
  if (pwdForm.value.newPassword !== pwdForm.value.confirmPassword)
    return ElMessage.warning('两次密码不一致')
  loading.value = true
  try { ElMessage.info('功能开发中，请组员接入接口') }
  finally { loading.value = false }
}
</script>

<template>
  <div style="max-width:560px">
    <h2>个人设置</h2>
    <el-card shadow="never" header="基本信息" style="margin-bottom:16px">
      <el-form label-width="80px">
        <el-form-item label="用户名">
          <el-input :model-value="auth.user?.username" disabled />
        </el-form-item>
        <el-form-item label="昵称">
          <el-input v-model="profileForm.nickname" placeholder="设置昵称" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="handleSaveProfile">保存</el-button>
        </el-form-item>
      </el-form>
    </el-card>
    <el-card shadow="never" header="修改密码">
      <el-form label-width="100px">
        <el-form-item label="当前密码">
          <el-input v-model="pwdForm.oldPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="新密码">
          <el-input v-model="pwdForm.newPassword" type="password" show-password />
        </el-form-item>
        <el-form-item label="确认新密码">
          <el-input v-model="pwdForm.confirmPassword" type="password" show-password />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="handleChangePassword">修改密码</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>
