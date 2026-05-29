<script setup lang="ts">
import { ref } from 'vue'
import type { DatabaseInfo } from '../api/litebase'

const props = defineProps<{
  columns: DatabaseInfo[]
}>()

const emit = defineEmits<{
  save: [rowid: unknown, body: Record<string, unknown>]
}>()

const open = ref(false)
const editRow = ref<Record<string, unknown>>({})
const saving = ref(false)
const error = ref<string | null>(null)

const openModal = (row: Record<string, unknown>) => {
  editRow.value = { ...row }
  error.value = null
  open.value = true
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
    open.value = false
  } finally {
    saving.value = false
  }
}

const setError = (msg: string) => { error.value = msg }

defineExpose({ openModal, setError })
</script>

<template>
  <UModal v-model:open="open" title="Edit Row">
    <template #body>
      <div class="flex flex-col gap-4">
        <div v-for="col in columns" :key="col.columnName" class="flex flex-col gap-1">
          <label class="text-sm font-medium text-neutral-700 dark:text-neutral-300">
            {{ col.columnName }}
            <span class="text-neutral-400 font-normal ml-1">({{ col.columnType }})</span>
            <span v-if="col.isPrimaryKey !== 0" class="text-xs text-blue-500 ml-1">PK</span>
          </label>
          <UInput v-if="col.columnType !== 'BLOB'"
            v-model="(editRow as Record<string, unknown>)[col.columnName] as string"
            :type="col.columnType === 'INTEGER' || col.columnType === 'REAL' ? 'number' : 'text'"
            :disabled="col.isPrimaryKey !== 0"
            class="w-full" />
          <UInput v-else disabled model-value="[BLOB data]" class="w-full opacity-50" />
        </div>
        <UAlert v-if="error" color="error" :description="error" />
      </div>
    </template>
    <template #footer>
      <div class="flex justify-end gap-2">
        <UButton color="neutral" variant="ghost" :disabled="saving" @click="open = false">Cancel</UButton>
        <UButton :loading="saving" @click="save">Save</UButton>
      </div>
    </template>
  </UModal>
</template>
