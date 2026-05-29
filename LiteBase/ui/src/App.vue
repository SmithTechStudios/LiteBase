<script setup lang="ts">
import { ref } from 'vue'
import axios from 'axios'
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
  <UApp>
    <div v-if="authLoading" class="flex items-center justify-center h-screen">
      <UIcon name="ph:spinner" class="animate-spin text-4xl" />
    </div>

    <Login v-else-if="!user" />

    <div v-else class="flex h-screen overflow-hidden">
      <Sidebar
        :table-names="uniqueTableNames"
        :selected-table="selectedTable"
        @select-table="selectedTable = $event"
        @logout="handleLogout"
      />
      <main class="flex-1 overflow-y-auto bg-default">
        <div class="container mx-auto px-8 py-10">
          <div v-if="isLoading" class="flex items-center justify-center py-16">
            <UIcon name="ph:spinner" class="animate-spin text-4xl" />
          </div>
          <div v-else-if="error" class="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <UIcon
              :name="axios.isAxiosError(error) && error.response?.status === 401 ? 'ph:lock' : 'ph:warning-circle'"
              class="text-5xl text-muted" />
            <p class="text-muted">{{ friendlyError(error) }}</p>
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
          <div v-else class="flex items-center justify-center py-16">
            <p class="text-muted">Select a table from the sidebar to view its data.</p>
          </div>
        </div>
      </main>
    </div>
  </UApp>
</template>
