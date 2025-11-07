<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { useDark, useToggle } from '@vueuse/core'
import axios from 'axios'
import Button from 'primevue/button'
import ProgressSpinner from 'primevue/progressspinner'
import InputText from 'primevue/inputtext'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import { Icon } from '@iconify/vue'
import { config } from './config'
import dayjs from 'dayjs'
import { onAuthStateChanged, signOut, type User } from 'firebase/auth'
import { auth } from './firebase'
import Login from './components/Login.vue'

interface DatabaseInfo {
  tableName: string
  columnName: string
  columnType: string
}

const searchQuery = ref('')
const selectedTable = ref<string | null>(null)
const isSidebarCollapsed = ref(false)
const user = ref<User | null>(null)
const authLoading = ref(true)

// Dark mode functionality
const isDark = useDark()
const toggleDark = useToggle(isDark)

const axiosClient = axios.create({
  baseURL: config.apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Authentication state management
onMounted(() => {
  onAuthStateChanged(auth, (firebaseUser: User | null) => {
    user.value = firebaseUser
    authLoading.value = false
    axiosClient.defaults.headers.common['Authorization'] = `Bearer ${firebaseUser?.accessToken}`
  })
})

const handleLogout = async () => {
  try {
    await signOut(auth)
  } catch (error) {
    console.error('Error signing out:', error)
  }
}

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
}

const { data: databaseInfo, isLoading, error } = useQuery({
  queryKey: ['databaseInfo'],
  queryFn: async () => {
    const response = await axiosClient.get<DatabaseInfo[]>(`/LiteBase/tables`)
    return response.data
  },
})

const uniqueTableNames = computed((): string[] => {
  const tableNames = databaseInfo.value?.map((info: DatabaseInfo) => info.tableName) || []
  return Array.from(new Set(tableNames)) as string[]
})

const filteredTableNames = computed((): string[] => {
  return uniqueTableNames.value.filter((name: string) =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const selectedTableColumns = computed(() => {
  return databaseInfo.value?.filter((info: DatabaseInfo) => info.tableName === selectedTable.value) || []
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
    const response = await axiosClient.get(`/LiteBase/table/${selectedTable.value}`)
    return response.data
  },
  enabled: computed(() => !!selectedTable.value),
})

</script>

<template>
  <!-- Show loading spinner while checking auth state -->
  <div v-if="authLoading" class="flex items-center justify-center h-screen">
    <ProgressSpinner />
  </div>

  <!-- Show login page if not authenticated -->
  <Login v-else-if="!user" />

  <!-- Show main app if authenticated -->
  <div v-else
    :class="['grid h-screen overflow-hidden bg-white transition-all duration-300', isSidebarCollapsed ? 'grid-cols-[4rem_1fr]' : 'grid-cols-[20rem_1fr]']">
    <!-- Sidebar -->
    <aside class="bg-neutral-100 border-r border-neutral-200 overflow-y-auto transition-all duration-300 flex flex-col">
      <div :class="['transition-all duration-300', isSidebarCollapsed ? 'p-2' : 'p-6']">
        <div :class="['flex items-center', isSidebarCollapsed ? 'justify-center' : 'justify-between']">
          <h1 v-show="!isSidebarCollapsed" class="text-2xl font-bold text-neutral-800 transition-opacity duration-300">
            Database Explorer</h1>
          <div class="flex gap-2">
            <Button @click="toggleSidebar" class="p-button-text p-button-rounded"
              :aria-label="isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'">
              <Icon :icon="isSidebarCollapsed ? 'ph:sidebar-simple-fill' : 'ph:sidebar-simple'" class="text-lg" />
            </Button>
            <Button v-show="!isSidebarCollapsed" @click="() => toggleDark()"
              class="p-button-text p-button-rounded transition-opacity duration-300">
              <Icon :icon="isDark ? 'ph:sun-bold' : 'ph:moon-bold'" />
            </Button>
            <Button v-show="!isSidebarCollapsed" @click="handleLogout"
              class="p-button-text p-button-rounded transition-opacity duration-300" title="Logout">
              <Icon icon="ph:sign-out" />
            </Button>
          </div>
        </div>
      </div>
      <div v-show="!isSidebarCollapsed" class="px-6 mb-6 transition-opacity duration-300">
        <InputText v-model="searchQuery" placeholder="Search tables"
          class="w-full text-neutral-700 border border-neutral-300 rounded-md bg-white placeholder-neutral-500" />
      </div>
      <nav class="transition-opacity duration-300 flex-1">
        <!-- Expanded view -->
        <ul v-show="!isSidebarCollapsed">
          <li v-for="tableName in filteredTableNames" :key="tableName" @click="selectTable(tableName)" :class="[
            'px-6 py-3 cursor-pointer transition-colors duration-200',
            selectedTable === tableName
              ? 'bg-blue-800/20 text-blue-600 font-medium'
              : 'text-neutral-600 hover:bg-neutral-50'
          ]">
            {{ tableName }}
          </li>
        </ul>
        <!-- Collapsed view - show icons only -->
        <ul v-show="isSidebarCollapsed" class="py-2">
          <li v-for="tableName in filteredTableNames" :key="tableName" @click="selectTable(tableName)" :class="[
            'flex items-center justify-center py-2 cursor-pointer transition-colors duration-200',
            selectedTable === tableName
              ? 'bg-blue-800/20 text-blue-600'
              : 'text-neutral-600 hover:bg-neutral-50'
          ]" :title="tableName">
            <Icon icon="ph:table" class="text-lg" />
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
