use lazy_static::lazy_static;
use ropey::Rope;
use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int};
use std::sync::Mutex;

lazy_static! {
    static ref ROPE: Mutex<Rope> = Mutex::new(Rope::new());
}

#[no_mangle]
pub extern "C" fn initialize_rope() {
    let mut rope = ROPE.lock().unwrap();
    *rope = Rope::new();
}

#[no_mangle]
pub extern "C" fn insert_text(index: c_int, text: *const c_char) {
    let text = unsafe { CStr::from_ptr(text) }.to_str().unwrap();
    let mut rope = ROPE.lock().unwrap();
    if index as usize <= rope.len_chars() {
        rope.insert(index as usize, text);
    }
}

#[no_mangle]
pub extern "C" fn delete_text(index: c_int, length: c_int) {
    let mut rope = ROPE.lock().unwrap();
    let start = index as usize;
    let end = start + length as usize;
    if start <= rope.len_chars() && end <= rope.len_chars() {
        rope.remove(start..end);
    }
}

#[no_mangle]
pub extern "C" fn get_rope_content() -> *mut c_char {
    let rope = ROPE.lock().unwrap();
    let content = rope.to_string();
    let c_content = CString::new(content).unwrap();
    c_content.into_raw()
}

#[no_mangle]
pub extern "C" fn get_rope_length() -> c_int {
    let rope = ROPE.lock().unwrap();
    rope.len_chars() as c_int
}

#[no_mangle]
pub extern "C" fn free_string(s: *mut c_char) {
    if s.is_null() {
        return;
    }
    unsafe { CString::from_raw(s) };
}
