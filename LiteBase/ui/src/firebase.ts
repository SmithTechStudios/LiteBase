// Import the functions you need from the SDKs you need
import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'

// Your web app's Firebase configuration
const firebaseConfig = {
  apiKey: 'AIzaSyCqB7VHM1-t_3gOrpXKUIB2gMs4DqirrpA',
  authDomain: 'bgquest.app',
  projectId: 'boardgame-tracker-2498e',
  storageBucket: 'boardgame-tracker-2498e.firebasestorage.app',
  messagingSenderId: '483970449590',
  appId: '1:483970449590:web:07fe9000eaf7b4bbf32617'
}

// Initialize Firebase
const app = initializeApp(firebaseConfig)
const auth = getAuth(app)

export { app, auth }

