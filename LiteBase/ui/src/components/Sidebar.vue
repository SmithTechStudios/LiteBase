<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  tableNames: string[]
  selectedTable: string | null
}>()

const emit = defineEmits<{
  selectTable: [tableName: string]
  logout: []
}>()

const searchQuery = ref('')
const isSidebarCollapsed = ref(false)

const colorMode = useColorMode()
const isDark = computed(() => colorMode.value === 'dark')
const toggleDark = () => { colorMode.preference = isDark.value ? 'light' : 'dark' }

const filteredTableNames = computed(() =>
  props.tableNames.filter(name =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
)
</script>

<template>
  <aside class="bg-neutral-100 dark:bg-neutral-900 border-r border-neutral-200 dark:border-neutral-800 overflow-y-auto transition-all duration-300 flex flex-col">
    <div :class="['transition-all duration-300', isSidebarCollapsed ? 'p-2' : 'p-6']">
      <div :class="['flex items-center', isSidebarCollapsed ? 'justify-center' : 'justify-between']">
        <h1 v-show="!isSidebarCollapsed" class="text-2xl font-bold text-neutral-800 dark:text-neutral-100 transition-opacity duration-300">
          Litebase
        </h1>
        <div class="flex gap-2">
          <UButton variant="ghost" :aria-label="isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'" @click="isSidebarCollapsed = !isSidebarCollapsed">
            <UIcon :name="isSidebarCollapsed ? 'ph:sidebar-simple-fill' : 'ph:sidebar-simple'" class="text-lg" />
          </UButton>
          <UButton v-show="!isSidebarCollapsed" variant="ghost" @click="toggleDark">
            <UIcon :name="isDark ? 'ph:sun-bold' : 'ph:moon-bold'" />
          </UButton>
          <UButton v-show="!isSidebarCollapsed" variant="ghost" title="Logout" @click="emit('logout')">
            <UIcon name="ph:sign-out" />
          </UButton>
        </div>
      </div>
    </div>

    <div v-show="!isSidebarCollapsed" class="px-6 mb-6">
      <UInput v-model="searchQuery" placeholder="Search tables" class="w-full" />
    </div>

    <nav class="flex-1">
      <ul v-show="!isSidebarCollapsed">
        <li v-for="tableName in filteredTableNames" :key="tableName"
          @click="emit('selectTable', tableName)"
          :class="['px-6 py-3 cursor-pointer transition-colors duration-200',
            selectedTable === tableName
              ? 'bg-blue-800/20 text-blue-600 font-medium'
              : 'text-neutral-600 hover:bg-neutral-50 dark:hover:bg-neutral-800']">
          {{ tableName }}
        </li>
      </ul>
      <ul v-show="isSidebarCollapsed" class="py-2">
        <li v-for="tableName in filteredTableNames" :key="tableName"
          @click="emit('selectTable', tableName)"
          :class="['flex items-center justify-center py-2 cursor-pointer transition-colors duration-200',
            selectedTable === tableName ? 'bg-blue-800/20 text-blue-600' : 'text-neutral-600 hover:bg-neutral-50 dark:hover:bg-neutral-800']"
          :title="tableName">
          <UIcon name="ph:table" class="text-lg" />
        </li>
      </ul>
    </nav>
  </aside>
</template>
