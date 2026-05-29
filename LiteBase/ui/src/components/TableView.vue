<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import { Icon } from '@iconify/vue'
import dayjs from 'dayjs'
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

const friendlyError = (err: unknown): string => {
  if (axios.isAxiosError(err) && err.response?.status === 401)
    return 'Access denied. You are not authorised to view this database.'
  return err instanceof Error ? err.message : 'An unexpected error occurred.'
}

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
  <div class="shadow-sm rounded-lg overflow-hidden border border-neutral-200 bg-neutral-100">
    <div
      class="px-6 py-4 border-b border-neutral-200 flex justify-between items-center bg-gradient-to-r from-blue-800/20 to-indigo-800/20">
      <h2 class="text-xl font-semibold text-neutral-800">{{ tableName }}</h2>
      <Button icon="pi pi-refresh" @click="emit('refetch')" :loading="tableLoading"
        class="p-button-text p-button-rounded p-button-plain text-neutral-600 hover:text-neutral-800" />
    </div>

    <div class="p-6">
      <h3 class="text-lg font-semibold mb-4 text-neutral-700">Table Columns</h3>
      <ul class="mb-8 grid grid-cols-2 gap-2">
        <li v-for="column in columns" :key="column.columnName" class="bg-neutral-200 rounded-md p-2 text-sm">
          <span class="font-medium text-neutral-700">{{ column.columnName }}</span>
          <span class="text-neutral-500 ml-2">({{ column.columnType }})</span>
        </li>
      </ul>

      <h3 class="text-lg font-semibold mb-4 text-neutral-700">Table Data</h3>
      <div v-if="tableLoading" class="flex justify-center">
        <ProgressSpinner />
      </div>
      <div v-else-if="tableError" class="flex flex-col items-center justify-center gap-3 mt-8 text-center">
        <Icon v-if="axios.isAxiosError(tableError) && tableError.response?.status === 401"
          icon="ph:lock-bold" class="text-4xl text-neutral-400" />
        <Icon v-else icon="ph:warning-circle-bold" class="text-4xl text-red-400" />
        <p class="text-neutral-600 font-medium">{{ friendlyError(tableError) }}</p>
      </div>
      <div v-else>
        <DataTable :value="tableData" :paginator="true" :rows="10" :rowsPerPageOptions="[10, 20, 50]"
          responsiveLayout="scroll"
          paginatorTemplate="CurrentPageReport FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown"
          currentPageReportTemplate="Showing {first} to {last} of {totalRecords}" class="p-datatable-sm" rowHover>
          <Column header="" style="width: 3rem" :exportable="false">
            <template #body="{ data }">
              <Button size="small" text rounded @click="editModal?.open(data as Record<string, unknown>)">
                <Icon icon="ph:pencil-simple" />
              </Button>
            </template>
          </Column>
          <Column v-for="col in visibleColumns" :key="col.columnName" :field="col.columnName"
            :header="col.columnName" sortable>
            <template #body="{ data, field }">
              <span v-if="col.columnType === 'TEXT' && data[field as keyof typeof data]?.includes('https://')">
                <img class="size-15" :src="data[field as keyof typeof data]" />
              </span>
              <span v-else-if="col.columnType === 'TEXT' && dayjs(data[field as keyof typeof data]).isValid()">
                {{ dayjs(data[field as keyof typeof data]).format('DD-MM-YYYY HH:mm') }}
              </span>
              <span v-else class="line-clamp-2 truncate">
                <span
                  v-if="typeof data[field as keyof typeof data] === 'number' && !Number.isInteger(data[field as keyof typeof data])">
                  {{ data[field as keyof typeof data]?.toFixed(2) }}
                </span>
                <span v-else>
                  {{ data[field as keyof typeof data]?.toString()?.substring(0, 200) }}
                  <span v-if="data[field as keyof typeof data]?.toString()?.length > 50">...</span>
                </span>
              </span>
            </template>
          </Column>
        </DataTable>

        <EditRowModal ref="editModal" :columns="visibleColumns" @save="handleSave" />
      </div>
    </div>
  </div>
</template>
