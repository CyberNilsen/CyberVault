using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace CyberVault.View
{
    public partial class SecureNotesControl : UserControl
    {
        private readonly string username;
        private readonly byte[] encryptionKey;
        private readonly string notesFilePath;
        private List<SimpleNote> notes;
        private List<SimpleNote> filteredNotes;
        private SimpleNote currentNote;
        private bool isLoading = false;

        public SecureNotesControl(string user, byte[] key)
        {
            InitializeComponent();
            username = user;
            encryptionKey = key;

            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberVault");
            Directory.CreateDirectory(appDataPath);
            notesFilePath = Path.Combine(appDataPath, $"{username}_notes.dat");

            notes = new List<SimpleNote>();
            filteredNotes = new List<SimpleNote>();
            LoadNotes();
            RefreshList();
        }

        private void LoadNotes()
        {
            try
            {
                if (File.Exists(notesFilePath))
                {
                    byte[] encrypted = File.ReadAllBytes(notesFilePath);
                    if (encrypted.Length > 0)
                    {
                        string json = DecryptData(encrypted);
                        notes = JsonSerializer.Deserialize<List<SimpleNote>>(json) ?? new List<SimpleNote>();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading notes: {ex.Message}");
                notes = new List<SimpleNote>();
            }
        }

        private void SaveNotes()
        {
            try
            {
                string json = JsonSerializer.Serialize(notes);
                byte[] encrypted = EncryptData(json);
                File.WriteAllBytes(notesFilePath, encrypted);
                StatusLabel.Text = "Saved";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving notes: {ex.Message}");
            }
        }

        private void RefreshList()
        {
            string searchText = SearchBox?.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredNotes = notes.ToList();
            }
            else
            {
                filteredNotes = notes.Where(n =>
                    n.Title.ToLower().Contains(searchText) ||
                    n.Content.ToLower().Contains(searchText)
                ).ToList();
            }

            var sortedNotes = filteredNotes.OrderByDescending(n => n.LastModified).ToList();
            NotesList.ItemsSource = sortedNotes;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshList();
        }

        private void NewNoteBtn_Click(object sender, RoutedEventArgs e)
        {
            var newNote = new SimpleNote
            {
                Id = Guid.NewGuid().ToString(),
                Title = "New Note",
                Content = "",
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };

            notes.Add(newNote);
            RefreshList();
            NotesList.SelectedItem = newNote;
            StatusLabel.Text = "New note created";
        }

        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentNote = NotesList.SelectedItem as SimpleNote;

            if (currentNote != null)
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                EditorContent.Visibility = Visibility.Visible;

                isLoading = true;
                TitleBox.Text = currentNote.Title;
                ContentBox.Text = currentNote.Content;
                isLoading = false;

                SaveBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
                StatusLabel.Text = $"Editing: {currentNote.Title}";
            }
            else
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                EditorContent.Visibility = Visibility.Collapsed;

                TitleBox.Text = "";
                ContentBox.Text = "";
                SaveBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;
                StatusLabel.Text = "Select a note to edit";
            }
        }

        private void TitleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isLoading && currentNote != null)
            {
                currentNote.Title = TitleBox.Text;
                currentNote.LastModified = DateTime.Now;
                StatusLabel.Text = "Modified";
            }
        }

        private void ContentBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isLoading && currentNote != null)
            {
                currentNote.Content = ContentBox.Text;
                currentNote.LastModified = DateTime.Now;
                StatusLabel.Text = "Modified";
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveNotes();
            RefreshList();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (currentNote != null)
            {
                var result = MessageBox.Show($"Delete '{currentNote.Title}'?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    notes.Remove(currentNote);
                    SaveNotes();
                    RefreshList();
                    NotesList.SelectedItem = null;
                    StatusLabel.Text = "Note deleted";
                }
            }
        }

        private byte[] EncryptData(string data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs))
                    {
                        writer.Write(data);
                    }
                    return ms.ToArray();
                }
            }
        }

        private string DecryptData(byte[] encryptedData)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = encryptionKey;

                byte[] iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, 16);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

    public class SimpleNote
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
    }
}