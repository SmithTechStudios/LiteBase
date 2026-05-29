<script setup lang="ts">
import { ref, computed } from 'vue'
import { useDark, useToggle } from '@vueuse/core'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import { Icon } from '@iconify/vue'

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

const isDark = useDark()
const toggleDark = useToggle(isDark)

const filteredTableNames = computed(() =>
  props.tableNames.filter((name) =>
    name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
)

const toggleSidebar = () => {
  isSidebarCollapsed.value = !isSidebarCollapsed.value
}
</script>

<template>
  <aside class="bg-neutral-100 border-r border-neutral-200 overflow-y-auto transition-all duration-300 flex flex-col">
    <div :class="['transition-all duration-300', isSidebarCollapsed ? 'p-2' : 'p-6']">
      <div :class="['flex items-center', isSidebarCollapsed ? 'justify-center' : 'justify-between']">
        <h1 v-show="!isSidebarCollapsed" class="text-2xl font-bold text-neutral-800 transition-opacity duration-300">
          Litebase
        </h1>
        <div class="flex gap-2">
          <Button @click="toggleSidebar" class="p-button-text p-button-rounded"
            :aria-label="isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'">
            <Icon :icon="isSidebarCollapsed ? 'ph:sidebar-simple-fill' : 'ph:sidebar-simple'" class="text-lg" />
          </Button>
          <Button v-show="!isSidebarCollapsed" @click="() => toggleDark()"
            class="p-button-text p-button-rounded transition-opacity duration-300">
            <Icon :icon="isDark ? 'ph:sun-bold' : 'ph:moon-bold'" />
          </Button>
          <Button v-show="!isSidebarCollapsed" @click="emit('logout')"
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
      <ul v-show="!isSidebarCollapsed">
        <li v-for="tableName in filteredTableNames" :key="tableName" @click="emit('selectTable', tableName)" :class="[
          'px-6 py-3 cursor-pointer transition-colors duration-200',
          selectedTable === tableName
            ? 'bg-blue-800/20 text-blue-600 font-medium'
            : 'text-neutral-600 hover:bg-neutral-50'
        ]">
          {{ tableName }}
        </li>
      </ul>
      <ul v-show="isSidebarCollapsed" class="py-2">
        <li v-for="tableName in filteredTableNames" :key="tableName" @click="emit('selectTable', tableName)" :class="[
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
</template>
