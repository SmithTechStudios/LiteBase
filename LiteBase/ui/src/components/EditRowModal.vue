<script setup lang="ts">
import { ref } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Dialog from 'primevue/dialog'
import type { DatabaseInfo } from '../api/litebase'

const props = defineProps<{
  columns: DatabaseInfo[]
}>()

const emit = defineEmits<{
  save: [rowid: unknown, body: Record<string, unknown>]
}>()

const visible = ref(false)
const editRow = ref<Record<string, unknown>>({})
const saving = ref(false)
const error = ref<string | null>(null)

const open = (row: Record<string, unknown>) => {
  editRow.value = { ...row }
  error.value = null
  visible.value = true
}

const save = async () => {
  saving.value = true
  error.value = null
  try {
    const rowid = editRow.value['rowid']
    const pkColumns = new Set(props.columns.filter(c => c.isPrimaryKey !== 0).map(c => c.columnName))
    const body = Object.fromEntries(
      Object.entries(editRow.value).filter(([k]) => k !== 'rowid' && !pkColumns.has(k))
    )
    emit('save', rowid, body)
    visible.value = false
  } finally {
    saving.value = false
  }
}

const setError = (msg: string) => { error.value = msg }

defineExpose({ open, setError })
</script>

<template>
  <Dialog v-model:visible="visible" modal header="Edit Row" :style="{ width: '32rem' }" :draggable="false">
    <div class="flex flex-col gap-4 pt-2">
      <div v-for="col in columns" :key="col.columnName" class="flex flex-col gap-1">
        <label class="text-sm font-medium text-neutral-700">
          {{ col.columnName }}
          <span class="text-neutral-400 font-normal ml-1">({{ col.columnType }})</span>
          <span v-if="col.isPrimaryKey !== 0" class="text-xs text-blue-500 ml-1">PK</span>
        </label>
        <InputText v-if="col.columnType !== 'BLOB'"
          v-model="(editRow as Record<string, unknown>)[col.columnName] as string"
          :type="col.columnType === 'INTEGER' || col.columnType === 'REAL' ? 'number' : 'text'"
          :disabled="col.isPrimaryKey !== 0"
          class="w-full" :class="{ 'opacity-50': col.isPrimaryKey !== 0 }" />
        <InputText v-else disabled value="[BLOB data]" class="w-full opacity-50" />
      </div>
      <div v-if="error" class="text-red-500 text-sm">{{ error }}</div>
    </div>
    <template #footer>
      <Button label="Cancel" severity="secondary" @click="visible = false" :disabled="saving" />
      <Button label="Save" @click="save" :loading="saving" />
    </template>
  </Dialog>
</template>
