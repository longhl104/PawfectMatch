import {
  Component,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
  ViewChild,
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
  FileUpload,
} from 'primeng/fileupload';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { ImageModule } from 'primeng/image';
import { MessageService } from 'primeng/api';
import {
  PetsApi,
  MediaFileUploadRequest,
  MediaFileType,
  UploadMediaFilesRequest,
  MediaFileUploadResponse,
  GetPetMediaResponse,
  DeleteMediaFilesRequest,
  DeleteMediaFilesResponse,
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
  imports: [FileUploadModule, ButtonModule, ProgressBarModule, ImageModule],
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

  @ViewChild('imageUpload') imageUploadRef!: FileUpload;
  @ViewChild('videoUpload') videoUploadRef!: FileUpload;
  @ViewChild('documentUpload') documentUploadRef!: FileUpload;

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

  // Individual upload states
  uploadingImages = false;
  uploadingVideos = false;
  uploadingDocuments = false;

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

  // Separate file handling methods for each media type
  onImageSelect(event: FileSelectEvent) {
    const files = Array.from(event.files) as File[];
    const imageFiles = files.filter((file) => file.type.startsWith('image/'));
    this.allNewFiles.update((current) => [...current, ...imageFiles]);
    this.emitMediaData();
  }

  onImageRemove(event: FileRemoveEvent) {
    const fileToRemove = event.file;
    this.removeFileFromCache(fileToRemove);
    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  onImagesClear() {
    const imagesToClear = this.newImages();
    imagesToClear.forEach((file) => this.removeFileFromCache(file));
    this.allNewFiles.update((current) =>
      current.filter((file) => !file.type.startsWith('image/')),
    );
    this.emitMediaData();
  }

  onVideoSelect(event: FileSelectEvent) {
    const files = Array.from(event.files) as File[];
    const videoFiles = files.filter((file) => file.type.startsWith('video/'));
    this.allNewFiles.update((current) => [...current, ...videoFiles]);
    this.emitMediaData();
  }

  onVideoRemove(event: FileRemoveEvent) {
    const fileToRemove = event.file;
    this.removeFileFromCache(fileToRemove);
    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  onVideosClear() {
    const videosTooClear = this.newVideos();
    videosTooClear.forEach((file) => this.removeFileFromCache(file));
    this.allNewFiles.update((current) =>
      current.filter((file) => !file.type.startsWith('video/')),
    );
    this.emitMediaData();
  }

  onDocumentSelect(event: FileSelectEvent) {
    const files = Array.from(event.files) as File[];
    const documentFiles = files.filter(
      (file) => this.getFileType(file) === 'document',
    );
    this.allNewFiles.update((current) => [...current, ...documentFiles]);
    this.emitMediaData();
  }

  onDocumentRemove(event: FileRemoveEvent) {
    const fileToRemove = event.file;
    this.removeFileFromCache(fileToRemove);
    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  onDocumentsClear() {
    const documentsTooClear = this.newDocuments();
    documentsTooClear.forEach((file) => this.removeFileFromCache(file));
    this.allNewFiles.update((current) =>
      current.filter((file) => this.getFileType(file) !== 'document'),
    );
    this.emitMediaData();
  }

  // Individual file removal methods
  removeImage(fileToRemove: File) {
    this.removeFileFromCache(fileToRemove);
    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  removeVideo(fileToRemove: File) {
    this.removeFileFromCache(fileToRemove);
    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  removeDocument(fileToRemove: File) {
    this.removeFileFromCache(fileToRemove);
    this.allNewFiles.update((current) =>
      current.filter((file) => file !== fileToRemove),
    );
    this.emitMediaData();
  }

  // Helper method for cache cleanup
  private removeFileFromCache(file: File) {
    const cachedUrl = this.blobUrlCache.get(file);
    if (cachedUrl) {
      URL.revokeObjectURL(cachedUrl);
      this.blobUrlCache.delete(file);
    }
  }

  // Unified file handling (legacy methods - keeping for compatibility)
  onFilesSelect(event: FileSelectEvent) {
    const files = Array.from(event.files) as File[];
    this.allNewFiles.update((current) => [...current, ...files]);
    this.emitMediaData();
  }

  onFileRemove(event: FileRemoveEvent) {
    const fileToRemove = event.file;
    this.removeFileFromCache(fileToRemove);
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
    this.removeFileFromCache(fileToRemove);
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

  async removeExistingImage(index: number) {
    const currentImages = this.displayExistingImages();
    const removedImage = currentImages[index];

    if (removedImage.id) {
      const petId = this.petId();
      if (!petId) {
        this.messageService.add({
          severity: 'error',
          summary: 'Delete Error',
          detail: 'Pet ID is required to delete media',
        });
        return;
      }

      try {
        // Call API to delete the media file
        const deleteRequest = new DeleteMediaFilesRequest({
          mediaFileIds: [removedImage.id]
        });

        const deleteResponse = await firstValueFrom(
          this.petsApi.mediaDELETE(petId, deleteRequest)
        ) as DeleteMediaFilesResponse;

        if (!deleteResponse.success) {
          this.messageService.add({
            severity: 'error',
            summary: 'Delete Error',
            detail: deleteResponse.errorMessage || 'Failed to delete image',
          });
          return;
        }

        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Image deleted successfully',
        });
      } catch (error) {
        console.error('Error deleting image:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Delete Error',
          detail: 'Failed to delete image. Please try again.',
        });
        return;
      }
    }

    this.displayExistingImages.update((images: MediaFile[]) =>
      images.filter((_, i) => i !== index),
    );
    this.emitMediaData();
  }

  async removeExistingVideo(index: number) {
    const currentVideos = this.displayExistingVideos();
    const removedVideo = currentVideos[index];

    if (removedVideo.id) {
      const petId = this.petId();
      if (!petId) {
        this.messageService.add({
          severity: 'error',
          summary: 'Delete Error',
          detail: 'Pet ID is required to delete media',
        });
        return;
      }

      try {
        // Call API to delete the media file
        const deleteRequest = new DeleteMediaFilesRequest({
          mediaFileIds: [removedVideo.id]
        });

        const deleteResponse = await firstValueFrom(
          this.petsApi.mediaDELETE(petId, deleteRequest)
        ) as DeleteMediaFilesResponse;

        if (!deleteResponse.success) {
          this.messageService.add({
            severity: 'error',
            summary: 'Delete Error',
            detail: deleteResponse.errorMessage || 'Failed to delete video',
          });
          return;
        }

        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Video deleted successfully',
        });
      } catch (error) {
        console.error('Error deleting video:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Delete Error',
          detail: 'Failed to delete video. Please try again.',
        });
        return;
      }
    }

    this.displayExistingVideos.update((videos: MediaFile[]) =>
      videos.filter((_, i) => i !== index),
    );
    this.emitMediaData();
  }

  async removeExistingDocument(index: number) {
    const currentDocuments = this.displayExistingDocuments();
    const removedDocument = currentDocuments[index];

    if (removedDocument.id) {
      const petId = this.petId();
      if (!petId) {
        this.messageService.add({
          severity: 'error',
          summary: 'Delete Error',
          detail: 'Pet ID is required to delete media',
        });
        return;
      }

      try {
        // Call API to delete the media file
        const deleteRequest = new DeleteMediaFilesRequest({
          mediaFileIds: [removedDocument.id]
        });

        const deleteResponse = await firstValueFrom(
          this.petsApi.mediaDELETE(petId, deleteRequest)
        ) as DeleteMediaFilesResponse;

        if (!deleteResponse.success) {
          this.messageService.add({
            severity: 'error',
            summary: 'Delete Error',
            detail: deleteResponse.errorMessage || 'Failed to delete document',
          });
          return;
        }

        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Document deleted successfully',
        });
      } catch (error) {
        console.error('Error deleting document:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Delete Error',
          detail: 'Failed to delete document. Please try again.',
        });
        return;
      }
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
      await this.uploadFiles(this.allNewFiles());

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

      // Clear all file upload components
      this.imageUploadRef?.clear();
      this.videoUploadRef?.clear();
      this.documentUploadRef?.clear();

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

  async uploadImages() {
    const imagesToUpload = this.newImages();
    if (imagesToUpload.length === 0) return;

    this.uploadingImages = true;

    try {
      await this.uploadFiles(imagesToUpload);

      // Remove uploaded images from the list
      this.allNewFiles.update((current) =>
        current.filter((file) => !file.type.startsWith('image/')),
      );

      // Clean up blob URLs for uploaded images
      imagesToUpload.forEach((file) => {
        const cachedUrl = this.blobUrlCache.get(file);
        if (cachedUrl) {
          URL.revokeObjectURL(cachedUrl);
          this.blobUrlCache.delete(file);
        }
      });

      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: `${imagesToUpload.length} image(s) uploaded successfully`,
      });

      // Clear the file upload component
      this.imageUploadRef?.clear();

      this.uploadComplete.emit();
      this.emitMediaData();
    } catch (error) {
      console.error('Image upload error:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Upload Error',
        detail: 'Failed to upload some images. Please try again.',
      });
    } finally {
      this.uploadingImages = false;
    }
  }

  async uploadVideos() {
    const videosToUpload = this.newVideos();
    if (videosToUpload.length === 0) return;

    this.uploadingVideos = true;

    try {
      await this.uploadFiles(videosToUpload);

      // Remove uploaded videos from the list
      this.allNewFiles.update((current) =>
        current.filter((file) => !file.type.startsWith('video/')),
      );

      // Clean up blob URLs for uploaded videos
      videosToUpload.forEach((file) => {
        const cachedUrl = this.blobUrlCache.get(file);
        if (cachedUrl) {
          URL.revokeObjectURL(cachedUrl);
          this.blobUrlCache.delete(file);
        }
      });

      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: `${videosToUpload.length} video(s) uploaded successfully`,
      });

      // Clear the file upload component
      this.videoUploadRef?.clear();

      this.uploadComplete.emit();
      this.emitMediaData();
    } catch (error) {
      console.error('Video upload error:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Upload Error',
        detail: 'Failed to upload some videos. Please try again.',
      });
    } finally {
      this.uploadingVideos = false;
    }
  }

  async uploadDocuments() {
    const documentsToUpload = this.newDocuments();
    if (documentsToUpload.length === 0) return;

    this.uploadingDocuments = true;

    try {
      await this.uploadFiles(documentsToUpload);

      // Remove uploaded documents from the list
      this.allNewFiles.update((current) =>
        current.filter((file) => this.getFileType(file) !== 'document'),
      );

      // Clean up blob URLs for uploaded documents
      documentsToUpload.forEach((file) => {
        const cachedUrl = this.blobUrlCache.get(file);
        if (cachedUrl) {
          URL.revokeObjectURL(cachedUrl);
          this.blobUrlCache.delete(file);
        }
      });

      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: `${documentsToUpload.length} document(s) uploaded successfully`,
      });

      // Clear the file upload component
      this.documentUploadRef?.clear();

      this.uploadComplete.emit();
      this.emitMediaData();
    } catch (error) {
      console.error('Document upload error:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Upload Error',
        detail: 'Failed to upload some documents. Please try again.',
      });
    } finally {
      this.uploadingDocuments = false;
    }
  }

  // Optimized batch upload method that uses uploadUrls for multiple files
  private async uploadFiles(files: File[]): Promise<void> {
    if (files.length === 0) return;

    const petId = this.petId();
    if (!petId) {
      throw new Error('Pet ID is required for file upload');
    }

    try {
      // Create media file requests for all files
      const mediaFileRequests: MediaFileUploadRequest[] = files.map((file) => {
        const fileType = this.getFileType(file);
        let mediaFileType: MediaFileType;

        switch (fileType) {
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
            throw new Error(`Unsupported file type: ${fileType}`);
        }

        return new MediaFileUploadRequest({
          fileName: file.name,
          contentType: file.type,
          fileSizeBytes: file.size,
          fileType: mediaFileType,
        });
      });

      // Create batch upload request
      const uploadRequest: UploadMediaFilesRequest =
        new UploadMediaFilesRequest({
          petId: petId,
          mediaFiles: mediaFileRequests,
        });

      // Get presigned upload URLs for all files in one request
      const uploadUrlResponse = (await firstValueFrom(
        this.petsApi.uploadUrls(petId, uploadRequest),
      )) as MediaFileUploadResponse;

      if (
        !uploadUrlResponse.success ||
        !uploadUrlResponse.uploadUrls ||
        uploadUrlResponse.uploadUrls.length === 0
      ) {
        throw new Error(
          uploadUrlResponse.errorMessage || 'Failed to get upload URLs',
        );
      }

      if (uploadUrlResponse.uploadUrls.length !== files.length) {
        throw new Error('Mismatch between files and upload URLs received');
      }

      // Update progress after getting URLs
      if (this.uploading) {
        this.uploadProgress = 20;
      }

      // Upload all files to S3 using their respective presigned URLs
      const uploadPromises = files.map(async (file, index) => {
        const uploadInfo = uploadUrlResponse.uploadUrls![index];

        if (!uploadInfo.presignedUrl) {
          throw new Error(`No presigned URL received for file: ${file.name}`);
        }

        const uploadResponse = await fetch(uploadInfo.presignedUrl, {
          method: 'PUT',
          body: file,
          headers: {
            'Content-Type': file.type,
          },
        });

        if (!uploadResponse.ok) {
          throw new Error(
            `Upload failed for ${file.name} with status: ${uploadResponse.status}`,
          );
        }

        return uploadInfo.mediaFileId!;
      });

      // Wait for all uploads to complete and collect media file IDs
      const uploadedMediaFileIds = await Promise.all(uploadPromises);

      // Update progress after S3 uploads complete
      if (this.uploading) {
        this.uploadProgress = 80;
      }

      // Confirm all uploads in one request
      const confirmResponse = (await firstValueFrom(
        this.petsApi.confirmUploads(petId, uploadedMediaFileIds),
      )) as GetPetMediaResponse;

      if (!confirmResponse.success) {
        throw new Error(
          confirmResponse.errorMessage || 'Failed to confirm uploads',
        );
      }

      // Complete progress
      if (this.uploading) {
        this.uploadProgress = 100;
      }

      console.log(`Successfully uploaded ${files.length} files`);
    } catch (error) {
      console.error('Error uploading files:', error);
      throw error;
    }
  }

  /**
   * @deprecated Use uploadFiles() instead for better performance with batch uploads
   * Legacy method for single file upload - kept for backward compatibility
   */
  private async uploadFile(file: File): Promise<void> {
    return this.uploadFiles([file]);
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
