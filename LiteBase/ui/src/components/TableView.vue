<script setup lang="ts">
import { ref, computed } from 'vue'
import axios from 'axios'
import dayjs from 'dayjs'
import type { TableColumn } from '@nuxt/ui'
import { updateRow, type DatabaseInfo } from '../api/litebase'
import EditRowModal from './EditRowModal.vue'

const props = defineProps<{
  tableName: string
  columns: DatabaseInfo[]
  visibleColumns: DatabaseInfo[]
  tableData: Record<string, unknown>[] | undefined
  tableLoading: boolean
  tableError: unknown
}>()

const emit = defineEmits<{
  refetch: []
}>()

const editModal = ref<InstanceType<typeof EditRowModal> | null>(null)

const page = ref(1)
const pageSize = ref(10)

const tableColumns = computed(() => [
  { id: 'actions', header: '', enableSorting: false },
  ...props.visibleColumns.map(col => ({ accessorKey: col.columnName, header: col.columnName, enableSorting: true }))
] as TableColumn<Record<string, unknown>>[])

const paginatedRows = computed(() => {
  const data = props.tableData ?? []
  const start = (page.value - 1) * pageSize.value
  return data.slice(start, start + pageSize.value)
})

const friendlyError = (err: unknown): string => {
  if (axios.isAxiosError(err) && err.response?.status === 401)
    return 'Access denied. You are not authorised to view this database.'
  return err instanceof Error ? err.message : 'An unexpected error occurred.'
}

const formatCell = (col: DatabaseInfo, value: unknown): string => {
  if (col.columnType === 'TEXT' && typeof value === 'string' && dayjs(value).isValid())
    return dayjs(value).format('DD-MM-YYYY HH:mm')
  if (typeof value === 'number' && !Number.isInteger(value))
    return value.toFixed(2)
  return String(value ?? '').substring(0, 200)
}

const isImageUrl = (col: DatabaseInfo, value: unknown) =>
  col.columnType === 'TEXT' && typeof value === 'string' && value.includes('https://')

const handleSave = async (rowid: unknown, body: Record<string, unknown>) => {
  try {
    await updateRow(props.tableName, rowid, body)
    emit('refetch')
  } catch (e: unknown) {
    editModal.value?.setError(e instanceof Error ? e.message : 'Failed to save changes')
  }
}
</script>

<template>
  <div class="shadow-sm rounded-lg overflow-hidden border border-neutral-200 dark:border-neutral-800 bg-white dark:bg-neutral-900">
    <div class="px-6 py-4 border-b border-neutral-200 dark:border-neutral-800 flex justify-between items-center bg-gradient-to-r from-blue-800/20 to-indigo-800/20">
      <h2 class="text-xl font-semibold text-neutral-800 dark:text-neutral-100">{{ tableName }}</h2>
      <UButton variant="ghost" icon="i-ph-arrows-clockwise" :loading="tableLoading" @click="emit('refetch')" />
    </div>

    <div class="p-6">
      <h3 class="text-lg font-semibold mb-4 text-neutral-700 dark:text-neutral-300">Table Columns</h3>
      <ul class="mb-8 grid grid-cols-2 gap-2">
        <li v-for="column in columns" :key="column.columnName"
          class="bg-neutral-100 dark:bg-neutral-800 rounded-md p-2 text-sm">
          <span class="font-medium text-neutral-700 dark:text-neutral-300">{{ column.columnName }}</span>
          <span class="text-neutral-500 ml-2">({{ column.columnType }})</span>
        </li>
      </ul>

      <h3 class="text-lg font-semibold mb-4 text-neutral-700 dark:text-neutral-300">Table Data</h3>

      <div v-if="tableLoading" class="flex justify-center py-8">
        <UIcon name="ph:spinner" class="animate-spin text-3xl text-neutral-400" />
      </div>
      <div v-else-if="tableError" class="flex flex-col items-center justify-center gap-3 mt-8 text-center">
        <UIcon
          :name="axios.isAxiosError(tableError) && tableError.response?.status === 401 ? 'ph:lock-bold' : 'ph:warning-circle-bold'"
          class="text-4xl text-neutral-400" />
        <p class="text-neutral-600 dark:text-neutral-400 font-medium">{{ friendlyError(tableError) }}</p>
      </div>
      <div v-else>
        <UTable :columns="tableColumns" :data="paginatedRows">
          <template #actions-cell="{ row }">
            <UButton variant="ghost" size="xs" icon="i-ph-pencil-simple"
              @click="editModal?.openModal(row.original as Record<string, unknown>)" />
          </template>
          <template v-for="col in visibleColumns" :key="col.columnName" #[`${col.columnName}-cell`]="{ row }">
            <img v-if="isImageUrl(col, row.original[col.columnName])" class="size-15" :src="row.original[col.columnName] as string" />
            <span v-else class="line-clamp-2 truncate">{{ formatCell(col, row.original[col.columnName]) }}</span>
          </template>
        </UTable>

        <div class="flex justify-between items-center mt-4">
          <USelect v-model="pageSize" :items="[10, 20, 50]" class="w-24" />
          <UPagination
            v-model:page="page"
            :total="tableData?.length ?? 0"
            :items-per-page="pageSize"
          />
        </div>

        <EditRowModal ref="editModal" :columns="visibleColumns" @save="handleSave" />
      </div>
    </div>
  </div>
</template>
