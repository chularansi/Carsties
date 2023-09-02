'use client';

import React, { useEffect, useState } from 'react';
import { AuctionCard } from './AuctionCard';
import { Auction, pagedResult } from '@/types';
import AppPagination from '../components/AppPagination';
import { getData } from '../actions/auctionActions';
import Filters from './Filters';
import { useParamsStore } from '../hooks/useParamsStore';
import { shallow } from 'zustand/shallow';
import qs from 'query-string';
import EmptyFilter from '../components/EmptyFilter';
import { useAuctionStore } from '../hooks/useAuctionStore';

export default function Listings() {
  const [loading, setLoading] = useState(true);
  // const [pageNumber, pageSize, searchTerm] = useParamsStore(
  //   (state) => [state.pageNumber, state.pageSize, state.searchTerm],
  //   shallow
  // );
  const params = useParamsStore(
    (state) => ({
      pageNumber: state.pageNumber,
      pageSize: state.pageSize,
      searchTerm: state.searchTerm,
      orderBy: state.orderBy,
      filterBy: state.filterBy,
      seller: state.seller,
      winner: state.winner,
    }),
    shallow
  );

  const data = useAuctionStore(
    (state) => ({
      auctions: state.auctions,
      totalCount: state.totalCount,
      pageCount: state.pageCount,
    }),
    shallow
  );

  const setParams = useParamsStore((state) => state.setParams);
  const setData = useAuctionStore((state) => state.setData);

  const url = qs.stringifyUrl({ url: '', query: params });

  function setPageNumber(pageNumber: number) {
    setParams({ pageNumber });
  }

  useEffect(() => {
    getData(url).then((data) => {
      setData(data);
      setLoading(false);
    });
  }, [url, setData]);

  if (loading) return <h3>Loading...</h3>;

  return (
    <>
      <Filters />
      {data.totalCount === 0 ? (
        <EmptyFilter showReset />
      ) : (
        <>
          <div className="grid grid-cols-4 gap-6">
            {data.auctions.map((auction) => (
              <AuctionCard auction={auction} key={auction.id} />
            ))}
          </div>
          <div className="flex justify-center mt-4">
            <AppPagination
              currentPage={params.pageNumber}
              pageCount={data.pageCount}
              pageChange={setPageNumber}
            />
          </div>
        </>
      )}
    </>
  );
}
