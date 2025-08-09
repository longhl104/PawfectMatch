export interface Pet {
  petPostgreSqlId: number;
  name: string;
  species: string;
  breed: string;
  age: number;
  gender: string;
  description: string;
  adoptionFee: number;
  location: string;
  distance: number;
  imageUrl: string;
  shelter: string;
  shelterPhone?: string;
  shelterWebsite?: string;
  shelterEmail?: string;
  isSpayedNeutered: boolean;
  isGoodWithKids: boolean;
  isGoodWithPets: boolean;
}
