use lazy_static::lazy_static;
use ropey::Rope;
use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int};
use std::sync::Mutex;

struct Piece {
    start: usize,
    length: usize,
    is_original: bool,
}

struct PieceTable {
    original: String,
    add_buffer: String,
    pieces: Vec<Piece>,
    total_length: usize,
}

impl PieceTable {
    fn new(content: String) -> Self {
        let length = content.len();
        PieceTable {
            original: content,
            add_buffer: String::new(),
            pieces: vec![Piece {
                start: 0,
                length,
                is_original: true,
            }],
            total_length: length,
        }
    }

    fn insert(&mut self, index: usize, text: &str) {
        if index > self.total_length {
            return;
        }

        let text_len = text.len();
        let mut offset = 0;
        let mut piece_index = 0;

        while piece_index < self.pieces.len() {
            let piece = &mut self.pieces[piece_index];
            if offset + piece.length > index {
                let split_point = index - offset;
                let new_piece = Piece {
                    start: self.add_buffer.len(),
                    length: text_len,
                    is_original: false,
                };
                self.add_buffer.push_str(text);

                if split_point < piece.length {
                    let after_piece = Piece {
                        start: piece.start + split_point,
                        length: piece.length - split_point,
                        is_original: piece.is_original,
                    };
                    piece.length = split_point;
                    self.pieces.insert(piece_index + 1, new_piece);
                    self.pieces.insert(piece_index + 2, after_piece);
                } else {
                    self.pieces.insert(piece_index + 1, new_piece);
                }

                self.total_length += text_len;
                return;
            }
            offset += piece.length;
            piece_index += 1;
        }

        // If we're here, we're inserting at the end
        self.pieces.push(Piece {
            start: self.add_buffer.len(),
            length: text_len,
            is_original: false,
        });
        self.add_buffer.push_str(text);
        self.total_length += text_len;
    }

    fn delete(&mut self, start: usize, length: usize) {
        if start + length > self.total_length {
            return;
        }

        let mut remaining = length;
        let mut offset = 0;
        let mut piece_index = 0;

        while remaining > 0 && piece_index < self.pieces.len() {
            let piece = &mut self.pieces[piece_index];
            if offset + piece.length > start {
                let delete_start = start.max(offset) - offset;
                let delete_end = (start + length).min(offset + piece.length) - offset;
                let delete_length = delete_end - delete_start;

                if delete_start == 0 && delete_length == piece.length {
                    self.pieces.remove(piece_index);
                } else {
                    if delete_start > 0 {
                        let new_piece = Piece {
                            start: piece.start + delete_end,
                            length: piece.length - delete_end,
                            is_original: piece.is_original,
                        };
                        piece.length = delete_start;
                        self.pieces.insert(piece_index + 1, new_piece);
                        piece_index += 1;
                    } else {
                        piece.start += delete_length;
                        piece.length -= delete_length;
                    }
                }

                remaining -= delete_length;
                self.total_length -= delete_length;
            } else {
                offset += piece.length;
                piece_index += 1;
            }
        }
    }

    fn to_string(&self) -> String {
        let mut result = String::with_capacity(self.total_length);
        for piece in &self.pieces {
            let slice = if piece.is_original {
                &self.original[piece.start..piece.start + piece.length]
            } else {
                &self.add_buffer[piece.start..piece.start + piece.length]
            };
            result.push_str(slice);
        }
        result
    }

    fn get_length(&self) -> usize {
        self.total_length
    }
}

lazy_static! {
    static ref DOCUMENT: Mutex<(Rope, PieceTable)> =
        Mutex::new((Rope::new(), PieceTable::new(String::new())));
}

#[no_mangle]
pub extern "C" fn initialize_document() {
    let mut doc = DOCUMENT.lock().unwrap();
    doc.0 = Rope::new();
    doc.1 = PieceTable::new(String::new());
}

#[no_mangle]
pub extern "C" fn insert_text(index: c_int, text: *const c_char) {
    let text = unsafe { CStr::from_ptr(text) }.to_str().unwrap();
    let mut doc = DOCUMENT.lock().unwrap();
    let index = index as usize;
    if index <= doc.0.len_chars() {
        doc.0.insert(index, text);
        doc.1.insert(index, text);
    }
}

#[no_mangle]
pub extern "C" fn delete_text(index: c_int, length: c_int) {
    let mut doc = DOCUMENT.lock().unwrap();
    let start = index as usize;
    let length = length as usize;
    if start + length <= doc.0.len_chars() {
        doc.0.remove(start..start + length);
        doc.1.delete(start, length);
    }
}

#[no_mangle]
pub extern "C" fn get_document_slice(start: c_int, end: c_int) -> *mut c_char {
    let doc = DOCUMENT.lock().unwrap();
    let start = start as usize;
    let end = end.min(doc.0.len_chars() as c_int) as usize;
    if start < end && end <= doc.0.len_chars() {
        let slice = doc.0.slice(start..end).to_string();
        CString::new(slice).unwrap().into_raw()
    } else {
        std::ptr::null_mut()
    }
}

#[no_mangle]
pub extern "C" fn get_document_length() -> c_int {
    let doc = DOCUMENT.lock().unwrap();
    doc.0.len_chars() as c_int
}

#[no_mangle]
pub extern "C" fn free_string(s: *mut c_char) {
    if !s.is_null() {
        unsafe { drop(CString::from_raw(s)) };
    }
}
