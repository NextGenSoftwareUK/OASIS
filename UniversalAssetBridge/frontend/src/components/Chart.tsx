"use client";

import { RwaChanges } from "@/types/rwa/rwa.type";
import {
  AreaSeries,
  ColorType,
  createChart,
  LineStyle,
  Time,
} from "lightweight-charts";
import { useEffect, useMemo, useRef } from "react";

interface ChartProps {
  firstData: RwaChanges;
  data: RwaChanges[];
  className?: string;
}

export default function Chart({ className, data, firstData }: ChartProps) {
  const chartContainerRef = useRef<HTMLDivElement>(null);
  const firstChangedData = useMemo(() => {
    return {
      time: (new Date(firstData.createdAt).getTime() / 1000) as Time,
      value: data?.[0]?.oldPrice,
    };
  }, [data, firstData]);

  const convertedData = useMemo(() => {
    if (!data || data.length === 0) {
      const date: Time = firstData?.createdAt || firstData.changedAt;
      const price: number = firstData?.price || firstData.newPrice;

      return [
        {
          time: (new Date(date).getTime() / 1000) as Time,
          value: price,
        },
      ];
    } else {
      const allData = [...data];
      const result = allData.map((item: RwaChanges) => {
        const date: Time = item?.createdAt || item.changedAt;
        const price: number = item?.price || item.newPrice;
        return {
          time: (new Date(date).getTime() / 1000) as Time,
          value: price,
        };
      });

      result.unshift(firstChangedData);
      return result;
    }
  }, [data, firstData, firstChangedData]);

  useEffect(() => {
    const chart = createChart(chartContainerRef.current!, {
      layout: {
        attributionLogo: false,
        background: {
          type: "solid" as ColorType,
          color: "transparent",
        },
        textColor: "#d1d4dc",
      },
      grid: {
        vertLines: {
          color: "rgba(255, 255, 255, 0.15)",
          style: LineStyle.LargeDashed,
        },
        horzLines: {
          color: "rgba(255, 255, 255, 0.15)",
          style: LineStyle.LargeDashed,
        },
      },
    });

    const candleSeries = chart.addSeries(AreaSeries, {
      lineColor: "#2962FF",
      topColor: "rgba(12, 18, 59, 0.6)",
      bottomColor: "rgba(12, 18, 59, 0.1)",
    });

    candleSeries.setData(convertedData);

    chart.timeScale().fitContent();
    return () => chart.remove();
  }, [convertedData]);

  return (
    <div className={`overflow-hidden px-2 rounded-2xl relative ${className}`}>
      <div
        className="w-full h-full flex justify-center items-center lg:h-96"
        ref={chartContainerRef}
      />
    </div>
  );
}
