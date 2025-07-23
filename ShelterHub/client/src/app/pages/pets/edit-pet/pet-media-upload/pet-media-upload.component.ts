import {
  Component,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
  input,
  effect,
} from '@angular/core';

import {
  FileUploadModule,
  FileSelectEvent,
  FileRemoveEvent,
} from 'primeng/fileupload';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { MessageService } from 'primeng/api';
import {
  PetsApi,
  MediaFileUploadRequest,
  MediaFileType,
  UploadMediaFilesRequest,
  MediaFileUploadResponse,
  GetPetMediaResponse,
} from '../../../../shared/apis/generated-apis';
import { firstValueFrom } from 'rxjs';

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
  imports: [FileUploadModule, ButtonModule, ProgressBarModule],
  templateUrl: './pet-media-upload.component.html',
  styleUrl: './pet-media-upload.component.scss',
})
export class PetMediaUploadComponent implements OnInit, OnDestroy {
  readonly petId = input<string | null>(null);
  readonly existingImages = input<MediaFile[]>([]);
  readonly existingVideos = input<MediaFile[]>([]);
  readonly existingDocuments = input<MediaFile[]>([]);

  @Output() mediaDataChange = new EventEmitter<MediaUploadData>();
  @Output() uploadComplete = new EventEmitter<void>();

  private messageService = inject(MessageService);
  private petsApi = inject(PetsApi);

  // Internal writable signals for existing media (to handle removals)
  readonly displayExistingImages = signal<MediaFile[]>([]);
  readonly displayExistingVideos = signal<MediaFile[]>([]);
  readonly displayExistingDocuments = signal<MediaFile[]>([]);

  // New files to upload (unified) - using signals
  allNewFiles = signal<File[]>([]);

  // Computed signals for different file types
  newImages = computed(() =>
    this.allNewFiles().filter((file) => file.type.startsWith('image/')),
  );

  newVideos = computed(() =>
    this.allNewFiles().filter((file) => file.type.startsWith('video/')),
  );

  newDocuments = computed(() =>
    this.allNewFiles().filter(
      (file) =>
        file.type === 'application/pdf' ||
        file.type.includes('document') ||
        file.type.includes('text') ||
        file.name.toLowerCase().endsWith('.pdf') ||
        file.name.toLowerCase().endsWith('.doc') ||
        file.name.toLowerCase().endsWith('.docx') ||
        file.name.toLowerCase().endsWith('.txt'),
    ),
  );

  // Removed existing media IDs
  removedMediaIds: string[] = [];

  // Cache for blob URLs to prevent recreation
  private blobUrlCache = new Map<File, string>();

  // Upload state
  uploading = false;
  uploadProgress = 0;

  constructor() {
    // Synchronize input signals with internal writable signals
    effect(() => {
      this.displayExistingImages.set([...this.existingImages()]);
    });

    effect(() => {
      this.displayExistingVideos.set([...this.existingVideos()]);
    });

    effect(() => {
      this.displayExistingDocuments.set([...this.existingDocuments()]);
    });
  }

  ngOnInit() {
    this.emitMediaData();
  }

  ngOnDestroy() {
    // Clean up all blob URLs to prevent memory leaks
    this.blobUrlCache.forEach((url) => URL.revokeObjectURL(url));
    this.blobUrlCache.clear();
  }

  // Unified file handling
  onFilesSelect(event: FileSelectEvent) {
    const files = Array.from(event.files) as File[];
    this.allNewFiles.update((current) => [...current, ...files]);
    this.emitMediaData();
  }

  onFileRemove(event: FileRemoveEvent) {
    const fileToRemove = event.file;

    // Clean up blob URL from cache
    const cachedUrl = this.blobUrlCache.get(fileToRemove);
    if (cachedUrl) {
      URL.revokeObjectURL(cachedUrl);
      this.blobUrlCache.delete(fileToRemove);
    }

    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  onFilesClear() {
    // Clean up all blob URLs from cache
    this.blobUrlCache.forEach((url) => URL.revokeObjectURL(url));
    this.blobUrlCache.clear();

    this.allNewFiles.set([]);
    this.emitMediaData();
  }

  removeFile(fileToRemove: File) {
    // Clean up blob URL from cache
    const cachedUrl = this.blobUrlCache.get(fileToRemove);
    if (cachedUrl) {
      URL.revokeObjectURL(cachedUrl);
      this.blobUrlCache.delete(fileToRemove);
    }

    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  hasExistingMedia(): boolean {
    return (
      this.displayExistingImages().length > 0 ||
      this.displayExistingVideos().length > 0 ||
      this.displayExistingDocuments().length > 0
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
    const currentImages = this.displayExistingImages();
    const removedImage = currentImages[index];
    if (removedImage.id) {
      this.removedMediaIds.push(removedImage.id);
    }
    this.displayExistingImages.update((images: MediaFile[]) =>
      images.filter((_, i) => i !== index),
    );
    this.emitMediaData();
  }

  removeExistingVideo(index: number) {
    const currentVideos = this.displayExistingVideos();
    const removedVideo = currentVideos[index];
    if (removedVideo.id) {
      this.removedMediaIds.push(removedVideo.id);
    }
    this.displayExistingVideos.update((videos: MediaFile[]) =>
      videos.filter((_, i) => i !== index),
    );
    this.emitMediaData();
  }

  removeExistingDocument(index: number) {
    const currentDocuments = this.displayExistingDocuments();
    const removedDocument = currentDocuments[index];
    if (removedDocument.id) {
      this.removedMediaIds.push(removedDocument.id);
    }
    this.displayExistingDocuments.update((documents: MediaFile[]) =>
      documents.filter((_, i) => i !== index),
    );
    this.emitMediaData();
  }

  // Utility methods
  getImagePreview(file: File): string {
    if (!this.blobUrlCache.has(file)) {
      this.blobUrlCache.set(file, URL.createObjectURL(file));
    }
    return this.blobUrlCache.get(file)!;
  }

  getVideoPreview(file: File): string {
    if (!this.blobUrlCache.has(file)) {
      this.blobUrlCache.set(file, URL.createObjectURL(file));
    }
    return this.blobUrlCache.get(file)!;
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
    return this.allNewFiles().length > 0;
  }

  async uploadAllFiles() {
    if (!this.hasNewFiles()) return;

    this.uploading = true;
    this.uploadProgress = 0;

    try {
      const totalFiles = this.allNewFiles().length;
      let completedFiles = 0;

      // Upload all files
      for (const file of this.allNewFiles()) {
        const fileType = this.getFileType(file);
        await this.uploadFile(file, fileType);
        completedFiles++;
        this.uploadProgress = Math.round((completedFiles / totalFiles) * 100);
      }

      // Clear new files after successful upload
      const filesToClear = [...this.allNewFiles()];
      this.allNewFiles.set([]);

      // Clean up blob URLs for uploaded files
      filesToClear.forEach((file) => {
        const cachedUrl = this.blobUrlCache.get(file);
        if (cachedUrl) {
          URL.revokeObjectURL(cachedUrl);
          this.blobUrlCache.delete(file);
        }
      });

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
    const petId = this.petId();
    if (!petId) {
      throw new Error('Pet ID is required for file upload');
    }

    try {
      // Convert file type to MediaFileType enum
      let mediaFileType: MediaFileType;
      switch (type) {
        case 'image':
          mediaFileType = MediaFileType.Image;
          break;
        case 'video':
          mediaFileType = MediaFileType.Video;
          break;
        case 'document':
          mediaFileType = MediaFileType.Document;
          break;
        default:
          throw new Error(`Unsupported file type: ${type}`);
      }

      // Create upload request
      const uploadRequest: UploadMediaFilesRequest =
        new UploadMediaFilesRequest({
          petId: petId,
          mediaFiles: [
            new MediaFileUploadRequest({
              fileName: file.name,
              contentType: file.type,
              fileSizeBytes: file.size,
              fileType: mediaFileType,
            }),
          ],
        });

      // Get presigned upload URL
      const uploadUrlResponse = (await firstValueFrom(
        this.petsApi.uploadUrls(petId, uploadRequest),
      )) as MediaFileUploadResponse;

      if (
        !uploadUrlResponse.success ||
        !uploadUrlResponse.uploadUrls ||
        uploadUrlResponse.uploadUrls.length === 0
      ) {
        throw new Error(
          uploadUrlResponse.errorMessage || 'Failed to get upload URL',
        );
      }

      const uploadInfo = uploadUrlResponse.uploadUrls[0];
      if (!uploadInfo.presignedUrl) {
        throw new Error('No presigned URL received');
      }

      // Upload file to S3 using presigned URL
      const uploadResponse = await fetch(uploadInfo.presignedUrl, {
        method: 'PUT',
        body: file,
        headers: {
          'Content-Type': file.type,
        },
      });

      if (!uploadResponse.ok) {
        throw new Error(`Upload failed with status: ${uploadResponse.status}`);
      }

      // Confirm upload
      const confirmResponse = (await firstValueFrom(
        this.petsApi.confirmUploads(petId, [uploadInfo.mediaFileId!]),
      )) as GetPetMediaResponse;

      if (!confirmResponse.success) {
        throw new Error(
          confirmResponse.errorMessage || 'Failed to confirm upload',
        );
      }

      console.log(`Successfully uploaded ${type}:`, file.name);
    } catch (error) {
      console.error(`Error uploading ${type} file:`, file.name, error);
      throw error;
    }
  }

  private emitMediaData() {
    const mediaData: MediaUploadData = {
      newImages: this.newImages(),
      newVideos: this.newVideos(),
      newDocuments: this.newDocuments(),
      existingImages: this.displayExistingImages(),
      existingVideos: this.displayExistingVideos(),
      existingDocuments: this.displayExistingDocuments(),
      removedMediaIds: this.removedMediaIds,
    };
    this.mediaDataChange.emit(mediaData);
  }
}
