<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/modules/auth'
import { authApi } from '@/api/auth'
import { ElMessage } from 'element-plus'

const auth = useAuthStore()
const loading = ref(false)
const profileForm = ref({ nickname: auth.user?.nickname || '' })
const pwdForm = ref({ oldPassword: '', newPassword: '', confirmPassword: '' })

async function handleSaveProfile() {
  if (!profileForm.value.nickname.trim()) return ElMessage.warning('昵称不能为空')
  loading.value = true
  try {
    const res = await authApi.updateProfile({ nickname: profileForm.value.nickname })
    auth.updateUser({ nickname: res.nickname })
    ElMessage.success('昵称修改成功')
  } finally { loading.value = false }
}

async function handleChangePassword() {
  if (!pwdForm.value.oldPassword || !pwdForm.value.newPassword)
    return ElMessage.warning('请填写完整密码信息')
  if (pwdForm.value.newPassword !== pwdForm.value.confirmPassword)
    return ElMessage.warning('两次输入的新密码不一致')
  if (pwdForm.value.newPassword.length < 6)
    return ElMessage.warning('新密码长度不能少于6位')
  loading.value = true
  try {
    await authApi.changePassword({
      oldPassword: pwdForm.value.oldPassword,
      newPassword: pwdForm.value.newPassword,
    })
    ElMessage.success('密码修改成功，请重新登录')
    pwdForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
    setTimeout(() => { auth.logout(); location.href = '/login' }, 1500)
  } finally { loading.value = false }
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
          <el-input v-model="profileForm.nickname" placeholder="设置昵称" maxlength="20" show-word-limit />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="handleSaveProfile">保存昵称</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card shadow="never" header="修改密码">
      <el-form label-width="100px">
        <el-form-item label="当前密码">
          <el-input v-model="pwdForm.oldPassword" type="password" show-password placeholder="请输入当前密码" />
        </el-form-item>
        <el-form-item label="新密码">
          <el-input v-model="pwdForm.newPassword" type="password" show-password placeholder="至少6位" />
        </el-form-item>
        <el-form-item label="确认新密码">
          <el-input v-model="pwdForm.confirmPassword" type="password" show-password placeholder="再次输入新密码" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="loading" @click="handleChangePassword">修改密码</el-button>
          <el-text type="info" style="margin-left:12px;font-size:12px">修改后将自动退出登录</el-text>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>
