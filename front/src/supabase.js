
import { createClient } from '@supabase/supabase-js'

// 🔹 Configuración para GMAIL
const supabaseUrlGmail = import.meta.env.supabaseUrlGmail
const supabaseAnonKeyGmail = import.meta.env.supabaseAnonKeyGmail

// 🔹 Configuración para UCB
const supabaseUrlUcb = import.meta.env.supabaseUrlUcb
const supabaseAnonKeyUcb = import.meta.env.supabaseAnonKeyUcb

// 🔹 Función para obtener el cliente correcto basado en el email
export function getSupabaseClient(email) {
  if (email.endsWith('@ucb.edu.bo')) {
    console.log('🔹 Usando cliente Supabase UCB para:', email)
    return createClient(supabaseUrlUcb, supabaseAnonKeyUcb)
  } else if (email.endsWith('@gmail.com')) {
    console.log('🔹 Usando cliente Supabase GMAIL para:', email)
    return createClient(supabaseUrlGmail, supabaseAnonKeyGmail)
  } else {
    console.log('🔹 Usando cliente Supabase por defecto (GMAIL) para:', email)
    return createClient(supabaseUrlGmail, supabaseAnonKeyGmail)
  }
}

// 🔹 Cliente por defecto (para inicialización)
export const supabase = createClient(supabaseUrlGmail, supabaseAnonKeyGmail)

// 🔹 Exportar clientes específicos
export const supabaseGmail = createClient(supabaseUrlGmail, supabaseAnonKeyGmail)
export const supabaseUcb = createClient(supabaseUrlUcb, supabaseAnonKeyUcb)