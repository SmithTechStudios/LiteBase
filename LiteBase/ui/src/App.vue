<script setup lang="ts">
import { ref, computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useDark, useToggle } from '@vueuse/core'
import axios from 'axios'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import InputText from 'primevue/inputtext'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import { Icon } from '@iconify/vue'
import { config } from './config'

interface DatabaseInfo {
  tableName: string
  columnName: string
  columnType: string
}

const searchQuery = ref('')
const selectedTable = ref<string | null>(null)

// Dark mode functionality
const isDark = useDark()
const toggleDark = useToggle(isDark)

const { data: databaseInfo, isLoading, error } = useQuery({
  queryKey: ['databaseInfo'],
  queryFn: async () => {
    const response = await axios.get<DatabaseInfo[]>(`${config.apiBaseUrl}/LiteBase/tables`)
    return response.data
  },
})

const uniqueTableNames = computed(() => {
  return [...new Set(databaseInfo.value?.map(info => info.tableName) || [])]
})

const filteredTableNames = computed(() => {
  return uniqueTableNames.value.filter(name =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const selectedTableColumns = computed(() => {
  return databaseInfo.value?.filter(info => info.tableName === selectedTable.value) || []
})

const selectTable = (tableName: string) => {
  selectedTable.value = tableName
}

const {
  data: tableData,
  isLoading: tableLoading,
  error: tableError,
  refetch: refetchTableData
} = useQuery({
  queryKey: ['tableData', selectedTable],
  queryFn: async () => {
    if (!selectedTable.value) return []
    const response = await axios.get(`${config.apiBaseUrl}/LiteBase/table/${selectedTable.value}`)
    return response.data
  },
  enabled: computed(() => !!selectedTable.value),
})

</script>

<template>
  <div class="grid grid-cols-[20rem_1fr] h-screen overflow-hidden bg-white">
    <!-- Sidebar -->
    <aside class="bg-neutral-100 border-r border-neutral-200 overflow-y-auto">
      <div class="p-6">
        <div class="flex items-center justify-between">
          <h1 class="text-2xl font-bold text-neutral-800">Database Explorer</h1>
          <Button @click="() => toggleDark()">
            <Icon :icon="isDark ? 'ph:sun-bold' : 'ph:moon-bold'" />
          </Button>
        </div>
      </div>
      <div class="px-6 mb-6">
        <InputText v-model="searchQuery" placeholder="Search tables"
          class="w-full text-neutral-700 border border-neutral-300 rounded-md bg-white placeholder-neutral-500" />
      </div>
      <nav>
        <ul>
          <li v-for="tableName in filteredTableNames" :key="tableName" @click="selectTable(tableName)" :class="[
            'px-6 py-3 cursor-pointer transition-colors duration-200',
            selectedTable === tableName
              ? 'bg-blue-800/20 text-blue-600 font-medium'
              : 'text-neutral-600 hover:bg-neutral-50'
          ]">
            {{ tableName }}
          </li>
        </ul>
      </nav>
    </aside>

    <!-- Main content -->
    <main class="bg-neutral-100 overflow-y-auto">
      <div class="container mx-auto px-8 py-10">
        <div v-if="isLoading" class="flex items-center justify-center h-full">
          <ProgressSpinner />
        </div>
        <div v-else-if="error" class="text-red-500">
          {{ (error as Error).message }}
        </div>
        <div v-else-if="selectedTable"
          class="shadow-sm rounded-lg overflow-hidden border border-neutral-200 bg-neutral-100">
          <div
            class="px-6 py-4 border-b border-neutral-200 flex justify-between items-center bg-gradient-to-r from-blue-800/20 to-indigo-800/20">
            <h2 class="text-xl font-semibold text-neutral-800">{{ selectedTable }}</h2>
            <Button icon="pi pi-refresh" @click="() => refetchTableData" :loading="tableLoading"
              class="p-button-text p-button-rounded p-button-plain text-neutral-600 hover:text-neutral-800" />
          </div>
          <div class="p-6">
            <h3 class="text-lg font-semibold mb-4 text-neutral-700">Table Columns</h3>
            <ul class="mb-8 grid grid-cols-2 gap-2">
              <li v-for="column in selectedTableColumns" :key="column.columnName"
                class="bg-neutral-200 rounded-md p-2 text-sm">
                <span class="font-medium text-neutral-700">{{ column.columnName }}</span>
                <span class="text-neutral-500 ml-2">({{ column.columnType }})</span>
              </li>
            </ul>
            <h3 class="text-lg font-semibold mb-4 text-neutral-700">Table Data</h3>
            <div v-if="tableLoading" class="flex justify-center">
              <ProgressSpinner />
            </div>
            <div v-else-if="tableError" class="text-red-500">
              {{ (tableError as Error).message }}
            </div>
            <div v-else>
              <DataTable :value="tableData" :paginator="true" :rows="10" :rowsPerPageOptions="[10, 20, 50]"
                responsiveLayout="scroll"
                paginatorTemplate="CurrentPageReport FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown"
                currentPageReportTemplate="Showing {first} to {last} of {totalRecords}" class="p-datatable-sm">
                <Column v-for="col in selectedTableColumns" :key="col.columnName" :field="col.columnName"
                  :header="col.columnName" sortable>
                </Column>
              </DataTable>
            </div>
          </div>
        </div>
        <div v-else class="text-center text-gray-500 mt-8">
          Select a table from the sidebar to view its data.
        </div>
      </div>
    </main>
  </div>
</template>

<style>
/* Custom scrollbar styles - these can't be easily replicated with Tailwind */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}

::-webkit-scrollbar-track {
  background: #f1f1f1;
}

::-webkit-scrollbar-thumb {
  background: #cbd5e0;
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: #a0aec0;
}
</style>
