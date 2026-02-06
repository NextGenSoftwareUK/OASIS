import { GridTileImage } from 'components/grid/tile';
import { getCategoryProducts, getProducts } from 'lib/medusa';
import type { Product } from 'lib/medusa/types';
import Link from 'next/link';

function ThreeItemGridItem({
  item,
  size,
  priority
}: {
  item: Product;
  size: 'full' | 'half';
  priority?: boolean;
}) {
  return (
    <div
      className={size === 'full' ? 'md:col-span-4 md:row-span-2' : 'md:col-span-2 md:row-span-1'}
    >
      <Link className="relative block aspect-square h-full w-full" href={`/product/${item.handle}`}>
        <GridTileImage
          src={item.featuredImage.url}
          fill
          sizes={
            size === 'full' ? '(min-width: 768px) 66vw, 100vw' : '(min-width: 768px) 33vw, 100vw'
          }
          priority={priority}
          alt={item.title}
          label={{
            position: size === 'full' ? 'center' : 'bottom',
            title: item.title as string,
            amount: item.priceRange.maxVariantPrice.amount,
            currencyCode: item.priceRange.maxVariantPrice.currencyCode
          }}
        />
      </Link>
    </div>
  );
}

export async function ThreeItemGrid() {
  // Prefer featured category; fall back to first 3 products so the shop shows something when Medusa is up.
  let homepageItems = await getCategoryProducts('hidden-homepage-featured-items');
  if (!homepageItems[0] || !homepageItems[1] || !homepageItems[2]) {
    const allProducts = await getProducts({});
    homepageItems = allProducts.slice(0, 3);
  }

  if (!homepageItems[0] || !homepageItems[1] || !homepageItems[2]) {
    return (
      <section className="mx-auto max-w-screen-2xl px-4 pb-16 pt-8">
        <div className="rounded-2xl border border-neutral-200 bg-neutral-50 p-16 text-center dark:border-neutral-800 dark:bg-neutral-900">
          <h2 className="mb-4 text-2xl font-semibold">Q2P Shop</h2>
          <p className="mb-6 text-neutral-600 dark:text-neutral-400">
            Quest-to-Physical: earn real-world discounts by completing in-world quests.
          </p>
          <p className="text-sm text-neutral-500">
            Connect the Medusa backend (check <code className="rounded bg-neutral-200 px-1 dark:bg-neutral-700">NEXT_PUBLIC_MEDUSA_BACKEND_API</code>) and ensure it’s running. If you just deployed, wait a minute for the server to start.
          </p>
        </div>
      </section>
    );
  }

  const [firstProduct, secondProduct, thirdProduct] = homepageItems;

  return (
    <section className="mx-auto grid max-w-screen-2xl gap-4 px-4 pb-4 md:grid-cols-6 md:grid-rows-2">
      <ThreeItemGridItem size="full" item={firstProduct} priority={true} />
      <ThreeItemGridItem size="half" item={secondProduct} priority={true} />
      <ThreeItemGridItem size="half" item={thirdProduct} />
    </section>
  );
}
