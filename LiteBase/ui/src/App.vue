<script setup lang="ts">
import { ref, computed } from 'vue'
import axios from 'axios'
import { useDark } from '@vueuse/core'
import type { NavigationMenuItem } from '@nuxt/ui'
import Login from './components/Login.vue'
import TableView from './components/TableView.vue'
import { useAuth } from './composables/useAuth'
import { useTableData } from './composables/useTableData'

const { user, authLoading, handleLogout } = useAuth()
const selectedTable = ref<string | null>(null)
const sidebarOpen = ref(true)

const isDark = useDark()
const toggleDark = () => { isDark.value = !isDark.value }

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

const navItems = computed<NavigationMenuItem[]>(() =>
  uniqueTableNames.value.map(name => ({
    label: name,
    icon: 'ph:table',
    active: selectedTable.value === name,
    onSelect: () => { selectedTable.value = name },
  }))
)

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
      <USidebar
        v-model:open="sidebarOpen"
        collapsible="icon"
        rail
        :ui="{ container: 'h-full' }"
      >
        <template #header>
          <span class="font-bold text-base truncate">Litebase</span>
        </template>

        <template #default="{ state }">
          <UNavigationMenu
            :key="state"
            :items="navItems"
            orientation="vertical"
            :ui="{ link: 'p-1.5 overflow-hidden' }"
          />
        </template>

        <template #footer>
          <div class="flex items-center gap-1">
            <UButton
              variant="ghost"
              color="neutral"
              :icon="isDark ? 'ph:sun-bold' : 'ph:moon-bold'"
              aria-label="Toggle dark mode"
              @click="toggleDark"
            />
            <UButton
              variant="ghost"
              color="neutral"
              icon="ph:sign-out"
              aria-label="Logout"
              @click="handleLogout"
            />
          </div>
        </template>
      </USidebar>

      <div class="flex-1 flex flex-col overflow-hidden">
        <div class="h-(--ui-header-height) shrink-0 flex items-center px-4 border-b border-default">
          <UButton
            icon="ph:sidebar-simple"
            color="neutral"
            variant="ghost"
            aria-label="Toggle sidebar"
            @click="sidebarOpen = !sidebarOpen"
          />
        </div>

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
    </div>
  </UApp>
</template>
