<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import InputText from 'primevue/inputtext'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import { config } from './config'

const databaseInfo = ref<DatabaseInfo[] | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const searchQuery = ref('')

onMounted(async () => {
  try {
    const response = await axios.get<DatabaseInfo[]>(`${config.apiBaseUrl}/LiteBase/tables`)
    databaseInfo.value = response.data
  } catch {
    console.error('Error fetching database info:', error)
    error.value = 'Failed to load database information. Please try again later.'
  } finally {
    loading.value = false
  }
})

interface DatabaseInfo {
  tableName: string
  columnName: string
  columnType: string
}

const uniqueTableNames = computed(() => {
  return [...new Set(databaseInfo.value?.map(info => info.tableName) || [])]
})

const filteredTableNames = computed(() => {
  return uniqueTableNames.value.filter(name =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const selectedTable = ref<string | null>(null)
const selectedTableColumns = computed(() => {
  return databaseInfo.value?.filter(info => info.tableName === selectedTable.value) || []
})

const selectTable = (tableName: string) => {
  selectedTable.value = tableName
  fetchTableData(tableName)
}

const tableData = ref<any[]>([])
const tableLoading = ref(false)
const tableError = ref<string | null>(null)

const fetchTableData = async (tableName: string) => {
  tableLoading.value = true
  tableError.value = null
  try {
    const response = await axios.get(`${config.apiBaseUrl}/LiteBase/table/${tableName}`)
    tableData.value = response.data.map((row: any) => {
      const formattedRow: { [key: string]: any } = {}
      selectedTableColumns.value.forEach((column, index) => {
        formattedRow[column.columnName] = row[index]
      })
      return formattedRow
    })
  } catch (error) {
    console.error(`Error fetching data for table ${tableName}:`, error)
    tableError.value = `Failed to load data for ${tableName}. Please try again.`
  } finally {
    tableLoading.value = false
  }
}

const refreshData = () => {
  if (selectedTable.value) {
    fetchTableData(selectedTable.value)
  }
}
</script>

<template>
  <div class="app-container">
    <!-- Sidebar -->
    <aside class="sidebar">
      <div class="p-6">
        <h1 class="text-2xl font-bold text-gray-800">Database Explorer</h1>
      </div>
      <div class="px-6 mb-6">
        <InputText v-model="searchQuery" placeholder="Search tables"
          class="w-full bg-white text-gray-700 border border-gray-300 rounded-md" />
      </div>
      <nav>
        <ul>
          <li v-for="tableName in filteredTableNames" :key="tableName" @click="selectTable(tableName)"
            :class="['px-6 py-3 cursor-pointer transition-colors duration-200',
              selectedTable === tableName ? 'bg-blue-50 text-blue-600 font-medium' : 'text-gray-600 hover:bg-gray-50']">
            {{ tableName }}
          </li>
        </ul>
      </nav>
    </aside>

    <!-- Main content -->
    <main class="main-content">
      <div class="container mx-auto px-8 py-10">
        <div v-if="loading" class="flex items-center justify-center h-full">
          <ProgressSpinner />
        </div>
        <div v-else-if="error" class="text-red-500">
          {{ error }}
        </div>
        <div v-else-if="selectedTable" class="shadow-sm rounded-lg overflow-hidden border border-gray-200">
          <div
            class="px-6 py-4 border-b border-gray-200 flex justify-between items-center bg-gradient-to-r from-blue-50 to-indigo-50">
            <h2 class="text-xl font-semibold text-gray-800">{{ selectedTable }}</h2>
            <Button icon="pi pi-refresh" @click="refreshData" :loading="tableLoading"
              class="p-button-text p-button-rounded p-button-plain" />
          </div>
          <div class="p-6">
            <h3 class="text-lg font-semibold mb-4 text-gray-700">Table Columns</h3>
            <ul class="mb-8 grid grid-cols-2 gap-2">
              <li v-for="column in selectedTableColumns" :key="column.columnName"
                class="bg-gray-50 rounded-md p-2 text-sm">
                <span class="font-medium text-gray-700">{{ column.columnName }}</span>
                <span class="text-gray-500 ml-2">({{ column.columnType }})</span>
              </li>
            </ul>
            <h3 class="text-lg font-semibold mb-4 text-gray-700">Table Data</h3>
            <div v-if="tableLoading" class="flex justify-center">
              <ProgressSpinner />
            </div>
            <div v-else-if="tableError" class="text-red-500">
              {{ tableError }}
            </div>
            <div v-else>
              <DataTable :value="tableData" :paginator="true" :rows="10" :rowsPerPageOptions="[10, 20, 50]"
                responsiveLayout="scroll"
                paginatorTemplate="CurrentPageReport FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown"
                currentPageReportTemplate="Showing {first} to {last} of {totalRecords}" class="p-datatable-sm">
                <Column v-for="col in selectedTableColumns" :key="col.columnName" :field="col.columnName"
                  :header="col.columnName" sortable>
                  <template #header="{ column }">
                    <span class="text-gray-700 font-semibold">{{ column.props.header }}</span>
                  </template>
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

<style scoped>
.app-container {
  display: grid;
  grid-template-columns: 20rem 1fr;
  grid-template-rows: 100vh;
  overflow: hidden;
}

.sidebar {
  grid-column: 1 / 2;
  background-color: #f8fafc;
  border-right: 1px solid #e2e8f0;
  overflow-y: auto;
}

.main-content {
  grid-column: 2 / 3;
  background-color: #ffffff;
  overflow-y: auto;
}

/* Scrollbar styles */
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
