<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
import ProgressSpinner from 'primevue/progressspinner'
import { Icon } from '@iconify/vue'
import Login from './components/Login.vue'
import Sidebar from './components/Sidebar.vue'
import TableView from './components/TableView.vue'
import { useAuth } from './composables/useAuth'
import { useTableData } from './composables/useTableData'

const { user, authLoading, handleLogout } = useAuth()

const selectedTable = ref<string | null>(null)

const {
  isLoading,
  error,
  uniqueTableNames,
  selectedTableColumns,
  visibleTableColumns,
  tableData,
  tableLoading,
  tableError,
  refetchTableData,
} = useTableData(selectedTable)

const friendlyError = (err: unknown): string => {
  if (axios.isAxiosError(err) && err.response?.status === 401)
    return 'Access denied. You are not authorised to view this database.'
  return err instanceof Error ? err.message : 'An unexpected error occurred.'
}
</script>

<template>
  <div v-if="authLoading" class="flex items-center justify-center h-screen">
    <ProgressSpinner />
  </div>

  <Login v-else-if="!user" />

  <div v-else class="grid h-screen overflow-hidden bg-white transition-all duration-300"
    style="grid-template-columns: auto 1fr">
    <Sidebar
      :table-names="uniqueTableNames"
      :selected-table="selectedTable"
      @select-table="selectedTable = $event"
      @logout="handleLogout"
    />

    <main class="bg-neutral-100 overflow-y-auto">
      <div class="container mx-auto px-8 py-10">
        <div v-if="isLoading" class="flex items-center justify-center h-full">
          <ProgressSpinner />
        </div>
        <div v-else-if="error" class="flex flex-col items-center justify-center gap-3 mt-16 text-center">
          <Icon v-if="axios.isAxiosError(error) && error.response?.status === 401"
            icon="ph:lock-bold" class="text-5xl text-neutral-400" />
          <Icon v-else icon="ph:warning-circle-bold" class="text-5xl text-red-400" />
          <p class="text-neutral-600 font-medium">{{ friendlyError(error) }}</p>
        </div>
        <TableView v-else-if="selectedTable"
          :table-name="selectedTable"
          :columns="selectedTableColumns"
          :visible-columns="visibleTableColumns"
          :table-data="tableData"
          :table-loading="tableLoading"
          :table-error="tableError"
          @refetch="refetchTableData"
        />
        <div v-else class="text-center text-gray-500 mt-8">
          Select a table from the sidebar to view its data.
        </div>
      </div>
    </main>
  </div>
</template>
