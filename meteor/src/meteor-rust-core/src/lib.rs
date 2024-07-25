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
}

impl PieceTable {
    fn new(content: String) -> Self {
        PieceTable {
            original: content.clone(),
            add_buffer: String::new(),
            pieces: vec![Piece {
                start: 0,
                length: content.len(),
                is_original: true,
            }],
        }
    }

    fn insert(&mut self, index: usize, text: &str) {
        if index > self.get_length() {
            return; // Prevent inserting at an invalid position
        }

        let mut remaining = text.len();
        let mut offset = 0;
        let mut piece_index = 0;

        while remaining > 0 && piece_index < self.pieces.len() {
            let piece = &self.pieces[piece_index];
            if offset + piece.length > index {
                // Split the piece
                let split_point = index - offset;
                let new_piece = Piece {
                    start: self.add_buffer.len(),
                    length: text.len(),
                    is_original: false,
                };
                self.add_buffer.push_str(text);

                let after_piece = Piece {
                    start: piece.start + split_point,
                    length: piece.length - split_point,
                    is_original: piece.is_original,
                };

                self.pieces[piece_index].length = split_point;
                self.pieces.insert(piece_index + 1, new_piece);
                self.pieces.insert(piece_index + 2, after_piece);

                break;
            }
            offset += piece.length;
            piece_index += 1;
        }
    }

    fn delete(&mut self, start: usize, length: usize) {
        if start + length > self.get_length() {
            return; // Prevent deleting beyond the end of the content
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
                    // Remove the entire piece
                    self.pieces.remove(piece_index);
                } else {
                    // Modify the piece
                    if delete_start > 0 {
                        let new_piece = Piece {
                            start: piece.start + delete_end,
                            length: piece.length - delete_end,
                            is_original: piece.is_original,
                        };
                        piece.length = delete_start;
                        self.pieces.insert(piece_index + 1, new_piece);
                    } else {
                        piece.start += delete_length;
                        piece.length -= delete_length;
                    }
                    piece_index += 1;
                }

                remaining -= delete_length;
            } else {
                offset += piece.length;
                piece_index += 1;
            }
        }
    }

    fn to_string(&self) -> String {
        let mut result = String::new();
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
        self.pieces.iter().map(|p| p.length).sum()
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
    if index as usize <= doc.0.len_chars() {
        doc.0.insert(index as usize, text);
        doc.1.insert(index as usize, text);
    }
}

#[no_mangle]
pub extern "C" fn delete_text(index: c_int, length: c_int) {
    let mut doc = DOCUMENT.lock().unwrap();
    let start = index as usize;
    let end = start + length as usize;
    if start <= doc.0.len_chars() && end <= doc.0.len_chars() {
        doc.0.remove(start..end);
        doc.1.delete(start, length as usize);
    }
}

#[no_mangle]
pub extern "C" fn get_document_slice(start: c_int, end: c_int) -> *mut c_char {
    let doc = DOCUMENT.lock().unwrap();
    let start = start as usize;
    let end = end.min(doc.0.len_chars() as c_int) as usize;
    if start < end && end <= doc.0.len_chars() {
        let slice = doc.0.slice(start..end).to_string();
        let c_slice = CString::new(slice).unwrap();
        c_slice.into_raw()
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
        unsafe { CString::from_raw(s) };
    }
}
