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
  <div class="flex flex-col gap-6">
    <div class="flex items-center justify-between">
      <h2 class="text-xl font-semibold">{{ tableName }}</h2>
      <UButton variant="ghost" color="neutral" icon="ph:arrows-clockwise" :loading="tableLoading" @click="emit('refetch')" />
    </div>

    <UCard :ui="{ root: 'shadow-lg' }">
      <template #header>
        <p class="font-medium">Table Columns</p>
      </template>
      <div class="grid grid-cols-2 gap-2">
        <UBadge v-for="column in columns" :key="column.columnName" variant="subtle" color="neutral" class="justify-start">
          <span class="font-medium">{{ column.columnName }}</span>
          <span class="opacity-60 ml-1">({{ column.columnType }})</span>
        </UBadge>
      </div>
    </UCard>

    <UCard :ui="{ root: 'shadow-lg' }">
      <template #header>
        <p class="font-medium">Table Data</p>
      </template>

      <div v-if="tableLoading" class="flex justify-center py-8">
        <UIcon name="ph:spinner" class="animate-spin text-3xl" />
      </div>
      <div v-else-if="tableError" class="flex flex-col items-center gap-3 py-8 text-center">
        <UIcon
          :name="axios.isAxiosError(tableError) && tableError.response?.status === 401 ? 'ph:lock' : 'ph:warning-circle'"
          class="text-4xl text-(--ui-text-muted)" />
        <p class="text-(--ui-text-muted)">{{ friendlyError(tableError) }}</p>
      </div>
      <template v-else>
        <UTable :columns="tableColumns" :data="paginatedRows">
          <template #actions-cell="{ row }">
            <UButton variant="ghost" color="neutral" size="xs" icon="ph:pencil-simple"
              @click="editModal?.openModal(row.original as Record<string, unknown>)" />
          </template>
          <template v-for="col in visibleColumns" :key="col.columnName" #[`${col.columnName}-cell`]="{ row }">
            <img v-if="isImageUrl(col, row.original[col.columnName])" class="size-10 rounded object-cover" :src="row.original[col.columnName] as string" />
            <span v-else class="line-clamp-2">{{ formatCell(col, row.original[col.columnName]) }}</span>
          </template>
        </UTable>

        <div class="flex justify-between items-center mt-4 pt-4 border-t border-(--ui-border)">
          <USelect v-model="pageSize" :items="[10, 20, 50]" class="w-20" />
          <UPagination v-model:page="page" :total="tableData?.length ?? 0" :items-per-page="pageSize" />
        </div>
      </template>

      <EditRowModal ref="editModal" :columns="visibleColumns" @save="handleSave" />
    </UCard>
  </div>
</template>
