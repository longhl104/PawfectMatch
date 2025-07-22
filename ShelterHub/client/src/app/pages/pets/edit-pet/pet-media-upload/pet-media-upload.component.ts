import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FileUploadModule,
  FileSelectEvent,
  FileRemoveEvent,
} from 'primeng/fileupload';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { MessageService } from 'primeng/api';

export interface MediaFile {
  id?: string;
  name: string;
  url: string;
  type: 'image' | 'video' | 'document';
  size?: number;
}

export interface MediaUploadData {
  newImages: File[];
  newVideos: File[];
  newDocuments: File[];
  existingImages: MediaFile[];
  existingVideos: MediaFile[];
  existingDocuments: MediaFile[];
  removedMediaIds: string[];
}

@Component({
  selector: 'app-pet-media-upload',
  standalone: true,
  imports: [CommonModule, FileUploadModule, ButtonModule, ProgressBarModule],
  templateUrl: './pet-media-upload.component.html',
  styleUrl: './pet-media-upload.component.scss',
})
export class PetMediaUploadComponent implements OnInit {
  @Input() petId: string | null = null;
  @Input() existingImages: MediaFile[] = [];
  @Input() existingVideos: MediaFile[] = [];
  @Input() existingDocuments: MediaFile[] = [];

  @Output() mediaDataChange = new EventEmitter<MediaUploadData>();
  @Output() uploadComplete = new EventEmitter<void>();

  private messageService = inject(MessageService);

  // New files to upload (unified)
  allNewFiles: File[] = [];

  // Computed arrays for different file types
  get newImages(): File[] {
    return this.allNewFiles.filter((file) => file.type.startsWith('image/'));
  }

  get newVideos(): File[] {
    return this.allNewFiles.filter((file) => file.type.startsWith('video/'));
  }

  get newDocuments(): File[] {
    return this.allNewFiles.filter(
      (file) =>
        file.type === 'application/pdf' ||
        file.type.includes('document') ||
        file.type.includes('text') ||
        file.name.toLowerCase().endsWith('.pdf') ||
        file.name.toLowerCase().endsWith('.doc') ||
        file.name.toLowerCase().endsWith('.docx') ||
        file.name.toLowerCase().endsWith('.txt'),
    );
  }

  // Removed existing media IDs
  removedMediaIds: string[] = [];

  // Upload state
  uploading = false;
  uploadProgress = 0;

  ngOnInit() {
    this.emitMediaData();
  }

  // Unified file handling
  onFilesSelect(event: FileSelectEvent) {
    const files = Array.from(event.files) as File[];
    this.allNewFiles = [...this.allNewFiles, ...files];
    this.emitMediaData();
  }

  onFileRemove(event: FileRemoveEvent) {
    const fileToRemove = event.file;
    this.allNewFiles = this.allNewFiles.filter((file) => file !== fileToRemove);
    this.emitMediaData();
  }

  onFilesClear() {
    this.allNewFiles = [];
    this.emitMediaData();
  }

  removeFile(fileToRemove: File) {
    this.allNewFiles = this.allNewFiles.filter((file) => file !== fileToRemove);
    this.emitMediaData();
  }

  // Utility methods for template
  getFilesByType(files: File[], type: 'image' | 'video' | 'document'): File[] {
    switch (type) {
      case 'image':
        return files.filter((file) => file.type.startsWith('image/'));
      case 'video':
        return files.filter((file) => file.type.startsWith('video/'));
      case 'document':
        return files.filter(
          (file) =>
            file.type === 'application/pdf' ||
            file.type.includes('document') ||
            file.type.includes('text') ||
            file.name.toLowerCase().endsWith('.pdf') ||
            file.name.toLowerCase().endsWith('.doc') ||
            file.name.toLowerCase().endsWith('.docx') ||
            file.name.toLowerCase().endsWith('.txt'),
        );
      default:
        return [];
    }
  }

  hasExistingMedia(): boolean {
    return (
      this.existingImages.length > 0 ||
      this.existingVideos.length > 0 ||
      this.existingDocuments.length > 0
    );
  }

  getFileType(file: File): 'image' | 'video' | 'document' {
    if (file.type.startsWith('image/')) {
      return 'image';
    } else if (file.type.startsWith('video/')) {
      return 'video';
    } else {
      return 'document';
    }
  }

  removeExistingImage(index: number) {
    const removedImage = this.existingImages[index];
    if (removedImage.id) {
      this.removedMediaIds.push(removedImage.id);
    }
    this.existingImages.splice(index, 1);
    this.emitMediaData();
  }

  removeExistingVideo(index: number) {
    const removedVideo = this.existingVideos[index];
    if (removedVideo.id) {
      this.removedMediaIds.push(removedVideo.id);
    }
    this.existingVideos.splice(index, 1);
    this.emitMediaData();
  }

  removeExistingDocument(index: number) {
    const removedDocument = this.existingDocuments[index];
    if (removedDocument.id) {
      this.removedMediaIds.push(removedDocument.id);
    }
    this.existingDocuments.splice(index, 1);
    this.emitMediaData();
  }

  // Utility methods
  getImagePreview(file: File): string {
    return URL.createObjectURL(file);
  }

  getVideoPreview(file: File): string {
    return URL.createObjectURL(file);
  }

  getDocumentIcon(fileName: string): string {
    const extension = fileName.split('.').pop()?.toLowerCase();
    switch (extension) {
      case 'pdf':
        return 'pi pi-file-pdf text-red-500';
      case 'doc':
      case 'docx':
        return 'pi pi-file-word text-blue-500';
      case 'txt':
        return 'pi pi-file text-gray-600';
      default:
        return 'pi pi-file text-gray-600';
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  hasNewFiles(): boolean {
    return this.allNewFiles.length > 0;
  }

  async uploadAllFiles() {
    if (!this.hasNewFiles()) return;

    this.uploading = true;
    this.uploadProgress = 0;

    try {
      const totalFiles = this.allNewFiles.length;
      let uploadedFiles = 0;

      // Simulate upload progress for now - replace with actual upload logic
      const updateProgress = () => {
        uploadedFiles++;
        this.uploadProgress = Math.round((uploadedFiles / totalFiles) * 100);
      };

      // Upload all files
      for (const file of this.allNewFiles) {
        const fileType = this.getFileType(file);
        await this.uploadFile(file, fileType);
        updateProgress();
      }

      // Clear new files after successful upload
      this.allNewFiles = [];

      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: 'All files uploaded successfully',
      });

      this.uploadComplete.emit();
      this.emitMediaData();
    } catch (error) {
      console.error('Upload error:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Upload Error',
        detail: 'Failed to upload some files. Please try again.',
      });
    } finally {
      this.uploading = false;
      this.uploadProgress = 0;
    }
  }

  private async uploadFile(
    file: File,
    type: 'image' | 'video' | 'document',
  ): Promise<void> {
    // Simulate upload delay - replace with actual upload logic
    return new Promise((resolve) => {
      setTimeout(() => {
        console.log(`Uploading ${type}:`, file.name);
        resolve();
      }, 1000);
    });
  }

  private emitMediaData() {
    const mediaData: MediaUploadData = {
      newImages: this.newImages,
      newVideos: this.newVideos,
      newDocuments: this.newDocuments,
      existingImages: this.existingImages,
      existingVideos: this.existingVideos,
      existingDocuments: this.existingDocuments,
      removedMediaIds: this.removedMediaIds,
    };
    this.mediaDataChange.emit(mediaData);
  }
}
