<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { refDebounced, useMediaQuery } from '@vueuse/core'
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

const isMobile = useMediaQuery('(max-width: 639px)')

const page = ref(1)
const pageSize = ref(10)
const search = ref('')
const debouncedSearch = refDebounced(search, 200)
const sortColumn = ref<string | null>(null)
const sortDirection = ref<'asc' | 'desc'>('asc')

const tableColumns = computed(() => [
  { id: 'actions', header: '' },
  ...props.visibleColumns.map(col => ({ id: col.columnName, accessorFn: (row: Record<string, unknown>) => row[col.columnName], header: col.columnName }))
] as TableColumn<Record<string, unknown>>[])

const filteredRows = computed(() => {
  const data = props.tableData ?? []
  const term = debouncedSearch.value.trim().toLowerCase()
  if (!term) return data
  return data.filter(row =>
    props.visibleColumns.some(col => {
      const value = row[col.columnName]
      if (value == null) return false
      return String(value).toLowerCase().includes(term) || formatCell(col, value).toLowerCase().includes(term)
    })
  )
})

const compareValues = (a: unknown, b: unknown): number => {
  if (typeof a === 'number' && typeof b === 'number') return a - b
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' })
}

const sortedRows = computed(() => {
  const column = sortColumn.value
  if (!column) return filteredRows.value
  const direction = sortDirection.value === 'asc' ? 1 : -1
  return [...filteredRows.value].sort((x, y) => {
    const a = x[column]
    const b = y[column]
    // Nulls always sort last, regardless of direction
    if (a == null && b == null) return 0
    if (a == null) return 1
    if (b == null) return -1
    return compareValues(a, b) * direction
  })
})

const paginatedRows = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return sortedRows.value.slice(start, start + pageSize.value)
})

const rangeStart = computed(() => sortedRows.value.length === 0 ? 0 : (page.value - 1) * pageSize.value + 1)
const rangeEnd = computed(() => Math.min(page.value * pageSize.value, sortedRows.value.length))

const toggleSort = (column: string) => {
  if (sortColumn.value !== column) {
    sortColumn.value = column
    sortDirection.value = 'asc'
  } else if (sortDirection.value === 'asc') {
    sortDirection.value = 'desc'
  } else {
    sortColumn.value = null
  }
}

const sortIcon = (column: string) => {
  if (sortColumn.value !== column) return 'ph:arrows-down-up'
  return sortDirection.value === 'asc' ? 'ph:sort-ascending' : 'ph:sort-descending'
}

watch([debouncedSearch, pageSize, sortColumn, sortDirection], () => { page.value = 1 })
watch(() => props.tableName, () => {
  search.value = ''
  sortColumn.value = null
  page.value = 1
})

const friendlyError = (err: unknown): string => {
  if (axios.isAxiosError(err) && err.response?.status === 401)
    return 'Access denied. You are not authorised to view this database.'
  return err instanceof Error ? err.message : 'An unexpected error occurred.'
}

// Only treat ISO-like strings as dates; dayjs happily parses e.g. 'Post 47' as the year 2047
const isoDatePattern = /^\d{4}-\d{2}-\d{2}/

const formatCell = (col: DatabaseInfo, value: unknown): string => {
  if (col.columnType === 'TEXT' && typeof value === 'string' && isoDatePattern.test(value) && dayjs(value).isValid())
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
  <div class="flex flex-col gap-4 sm:gap-6">
    <div class="flex items-center justify-between gap-2">
      <h2 class="text-lg sm:text-xl font-semibold truncate">{{ tableName }}</h2>
      <UButton variant="ghost" color="neutral" icon="ph:arrows-clockwise" aria-label="Refresh" :loading="tableLoading" @click="emit('refetch')" />
    </div>

    <UCollapsible :default-open="!isMobile" :unmount-on-hide="false">
      <UButton variant="ghost" color="neutral" class="w-full justify-between px-0 font-medium" trailing-icon="ph:caret-down"
        :ui="{ trailingIcon: 'group-data-[state=open]:rotate-180 transition-transform duration-200' }">
        Table Columns ({{ columns.length }})
      </UButton>
      <template #content>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2 pt-2">
          <UBadge v-for="column in columns" :key="column.columnName" variant="subtle" color="neutral" class="justify-start min-w-0">
            <span class="font-medium truncate">{{ column.columnName }}</span>
            <span class="opacity-60 ml-1 shrink-0">({{ column.columnType }})</span>
          </UBadge>
        </div>
      </template>
    </UCollapsible>

    <UCard :ui="{ root: 'shadow-lg', header: 'p-3 sm:px-6', body: 'p-0 sm:p-0' }">
      <template #header>
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <p class="font-medium">Table Data</p>
          <UInput v-model="search" icon="ph:magnifying-glass" placeholder="Search rows..." class="w-full sm:w-72"
            :ui="{ trailing: 'pe-1' }">
            <template v-if="search" #trailing>
              <UButton color="neutral" variant="link" size="sm" icon="ph:x" aria-label="Clear search" @click="search = ''" />
            </template>
          </UInput>
        </div>
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
        <UTable :columns="tableColumns" :data="paginatedRows" sticky
          :empty="debouncedSearch ? `No rows match '${debouncedSearch}'` : 'No rows in this table'"
          :ui="{ root: 'max-h-[70vh]', th: 'px-2 sm:px-4 whitespace-nowrap', td: 'px-2 sm:px-4 py-2 sm:py-3 max-w-xs' }">
          <template #actions-cell="{ row }">
            <UButton variant="ghost" color="neutral" size="xs" icon="ph:pencil-simple" aria-label="Edit row"
              @click="editModal?.openModal(row.original as Record<string, unknown>)" />
          </template>
          <template v-for="col in visibleColumns" :key="`${col.columnName}-header`" #[`${col.columnName}-header`]>
            <UButton variant="ghost" color="neutral" size="sm" class="-mx-2.5 font-semibold"
              :label="col.columnName"
              :trailing-icon="sortIcon(col.columnName)"
              :ui="{ trailingIcon: sortColumn === col.columnName ? 'text-primary' : 'opacity-40' }"
              :aria-label="`Sort by ${col.columnName}`"
              @click="toggleSort(col.columnName)" />
          </template>
          <template v-for="col in visibleColumns" :key="col.columnName" #[`${col.columnName}-cell`]="{ row }">
            <img v-if="isImageUrl(col, row.original[col.columnName])" class="size-10 rounded object-cover" :src="row.original[col.columnName] as string" />
            <span v-else class="line-clamp-2 break-words">{{ formatCell(col, row.original[col.columnName]) }}</span>
          </template>
        </UTable>

        <div class="flex flex-wrap justify-between items-center gap-3 p-3 sm:px-6 border-t border-(--ui-border)">
          <div class="flex items-center gap-2 text-sm text-(--ui-text-muted)">
            <USelect v-model="pageSize" :items="[10, 20, 50, 100]" class="w-20" aria-label="Rows per page" />
            <span>{{ rangeStart }}–{{ rangeEnd }} of {{ sortedRows.length }}<template v-if="debouncedSearch"> (filtered from {{ tableData?.length ?? 0 }})</template></span>
          </div>
          <UPagination v-model:page="page" :total="sortedRows.length" :items-per-page="pageSize"
            :sibling-count="isMobile ? 0 : 1" :show-edges="!isMobile" size="sm" />
        </div>
      </template>

      <EditRowModal ref="editModal" :columns="visibleColumns" @save="handleSave" />
    </UCard>
  </div>
</template>
