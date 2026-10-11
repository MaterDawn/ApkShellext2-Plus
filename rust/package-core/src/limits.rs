#[derive(Clone, Copy, Debug)]
pub struct Limits {
    pub max_entries: usize,
    pub max_entry_size: u64,
    pub max_declared_size: u64,
}

impl Default for Limits {
    fn default() -> Self {
        Self {
            max_entries: 65_536,
            max_entry_size: 64 * 1024 * 1024,
            max_declared_size: 2 * 1024 * 1024 * 1024,
        }
    }
}
