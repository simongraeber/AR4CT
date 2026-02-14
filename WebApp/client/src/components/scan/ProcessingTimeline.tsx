import React from "react";
import { motion, type Variants } from "framer-motion";
import {
  Check,
  Loader2,
  Upload,
  Cpu,
  Box,
  CircleCheck,
  AlertCircle,
  Clock,
  RefreshCw,
} from "lucide-react";
import {
  HoverCard,
  HoverCardTrigger,
  HoverCardContent,
} from "@/components/ui/hover-card";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { ScanStatus } from "./api";

interface StepDef {
  label: string;
  shortDesc: string;
  hoverTitle: string;
  hoverBody: string;
  activeHint?: string;
  icon: React.ReactNode;
}

const STEPS: StepDef[] = [
  {
    label: "Uploaded",
    shortDesc: "File received",
    hoverTitle: "CT Scan Uploaded",
    hoverBody: "Your CT scan file has been uploaded to the server and is ready for processing.",
    icon: <Upload className="w-5 h-5" />,
  },
  {
    label: "Segmentation",
    shortDesc: "AI processing",
    hoverTitle: "Organ Segmentation",
    hoverBody:
      "An AI model (TotalSegmentator) is segmenting your CT scan into individual organs on a remote GPU server.",
    activeHint: "This step can take up to 60 minutes depending on scan size and server queue length.",
    icon: <Cpu className="w-5 h-5" />,
  },
  {
    label: "3D Model",
    shortDesc: "Creating FBX",
    hoverTitle: "3D Model Generation",
    hoverBody:
      "The segmented organ meshes are combined into a single coloured 3D model (FBX) using Blender.",
    activeHint: "This usually takes 1–5 minutes.",
    icon: <Box className="w-5 h-5" />,
  },
  {
    label: "Ready",
    shortDesc: "View in AR",
    hoverTitle: "Processing Complete",
    hoverBody:
      "Your 3D model is ready! Download the FBX, print the AR sheet, or open it in the AR app.",
    icon: <CircleCheck className="w-5 h-5" />,
  },
];

type StepState = "completed" | "active" | "pending" | "error";

function getStepStates(
  status: ScanStatus,
  organsProcessed?: string[],
): StepState[] {
  switch (status) {
    case "uploaded":
      return ["completed", "pending", "pending", "pending"];
    case "processing":
      return ["completed", "active", "pending", "pending"];
    case "segmented":
      return ["completed", "completed", "pending", "pending"];
    case "post_processing":
      return ["completed", "completed", "active", "pending"];
    case "completed":
      return ["completed", "completed", "completed", "completed"];
    case "error": {
      const segDone = organsProcessed && organsProcessed.length > 0;
      return segDone
        ? ["completed", "completed", "error", "pending"]
        : ["completed", "error", "pending", "pending"];
    }
    default:
      return ["pending", "pending", "pending", "pending"];
  }
}

const stepVariants: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: (i: number) => ({
    opacity: 1,
    y: 0,
    transition: { delay: i * 0.25, duration: 0.7 },
  }),
};

const lineVariants: Variants = {
  hidden: { scaleX: 0 },
  visible: (i: number) => ({
    scaleX: 1,
    transition: { delay: i * 0.25, duration: 0.6 },
  }),
};

const circlePop: Variants = {
  hidden: { scale: 0.85, opacity: 0 },
  visible: (i: number) => ({
    scale: 1,
    opacity: 1,
    transition: {
      delay: i * 0.25 + 0.1,
      duration: 0.5,
    },
  }),
};

const glowRing: Variants = {
  animate: {
    scale: [1, 1.15, 1.4],
    opacity: [0, 0.35, 0],
    transition: { duration: 2.5, repeat: Infinity, repeatDelay: 0.5 },
  },
};

function circleClasses(state: StepState) {
  const base =
    "relative z-10 flex items-center justify-center w-10 h-10 rounded-full border-2 transition-colors duration-500 cursor-default";
  switch (state) {
    case "completed":
      return `${base} bg-primary border-primary text-primary-foreground`;
    case "active":
      return `${base} border-primary text-primary bg-background`;
    case "error":
      return `${base} border-destructive text-destructive bg-background`;
    default:
      return `${base} border-muted-foreground/30 text-muted-foreground/40 bg-background`;
  }
}

function labelClasses(state: StepState) {
  const base = "mt-2 text-xs font-medium text-center transition-colors";
  if (state === "completed" || state === "active") return `${base} text-foreground`;
  if (state === "error") return `${base} text-destructive`;
  return `${base} text-muted-foreground/50`;
}

interface ProcessingTimelineProps {
  status: ScanStatus;
  error?: string | null;
  organsProcessed?: string[];
  onRetry?: () => void;
  retrying?: boolean;
}

export default function ProcessingTimeline({
  status,
  error,
  organsProcessed,
  onRetry,
  retrying,
}: ProcessingTimelineProps) {
  const states = getStepStates(status, organsProcessed);

  return (
    <div className="w-full">
      <div className="flex items-start justify-between">
        {STEPS.map((step, i) => {
          const state = states[i];
          const prevCompleted = i > 0 && states[i - 1] === "completed";

          return (
            <HoverCard key={step.label} openDelay={200} closeDelay={100}>
              <motion.div
                className="flex-1 flex flex-col items-center relative"
                custom={i}
                variants={stepVariants}
                initial="hidden"
                animate="visible"
              >
                {/* Connector line (grows left→right) */}
                {i > 0 && (
                  <motion.div
                    className={cn(
                      "absolute top-5 right-1/2 w-full h-[3px] -translate-y-px origin-left",
                      prevCompleted ? "bg-primary" : "bg-border",
                    )}
                    style={{ zIndex: 0 }}
                    custom={i}
                    variants={lineVariants}
                    initial="hidden"
                    animate="visible"
                  />
                )}

                {/* Circle wrapper (glow ring + pop) */}
                <div className="relative">
                  {/* Animated glow ring for the active step */}
                  {state === "active" && (
                    <motion.div
                      className="absolute inset-0 rounded-full border-2 border-primary"
                      variants={glowRing}
                      animate="animate"
                    />
                  )}

                  {/* Circle (trigger) */}
                  <HoverCardTrigger asChild>
                    <motion.button
                      type="button"
                      className={circleClasses(state)}
                      custom={i}
                      variants={circlePop}
                      initial="hidden"
                      animate="visible"
                      aria-label={`${step.label}: ${step.shortDesc}`}
                    >
                      {state === "completed" ? (
                        <Check className="w-5 h-5" />
                      ) : state === "error" ? (
                        <AlertCircle className="w-5 h-5" />
                      ) : (
                        step.icon
                      )}
                    </motion.button>
                  </HoverCardTrigger>
                </div>

                {/* Label */}
                <p className={labelClasses(state)}>{step.label}</p>

                <HoverCardContent
                  side="top"
                  sideOffset={12}
                  className="w-72"
                >
                  <div className="space-y-2">
                    <h4 className="text-sm font-semibold flex items-center gap-1.5">
                      {state === "active" && (
                        <RefreshCw className="w-3.5 h-3.5 animate-spin text-primary" />
                      )}
                      {state === "error" && (
                        <AlertCircle className="w-3.5 h-3.5 text-destructive" />
                      )}
                      {step.hoverTitle}
                    </h4>
                    <p className="text-xs text-muted-foreground leading-relaxed">
                      {step.hoverBody}
                    </p>


                    {state === "active" && step.activeHint && (
                      <div className="flex items-start gap-1.5 text-xs text-primary/80 bg-primary/5 rounded-md p-2">
                        <Clock className="w-3.5 h-3.5 mt-0.5 shrink-0" />
                        <span>{step.activeHint}</span>
                      </div>
                    )}

                    {state === "error" && error && (
                      <div className="flex items-start gap-1.5 text-xs text-destructive bg-destructive/5 rounded-md p-2">
                        <AlertCircle className="w-3.5 h-3.5 mt-0.5 shrink-0" />
                        <span>{error}</span>
                      </div>
                    )}
                  </div>
                </HoverCardContent>
              </motion.div>
            </HoverCard>
          );
        })}
      </div>

      {(status === "processing" || status === "post_processing") && (
        <div className="mt-4 h-1.5 rounded-full bg-secondary overflow-hidden">
          <div className="h-full bg-primary/60 rounded-full animate-progress-indeterminate" />
        </div>
      )}

      {status === "error" && error && (
        <div className="mt-4 p-3 rounded-lg bg-destructive/10 border border-destructive/20 text-sm text-destructive flex items-start gap-2">
          <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
          <span>{error}</span>
        </div>
      )}

      {onRetry && (status === "error" || status === "uploaded" || status === "segmented") && (
        <div className="mt-4 flex justify-center">
          <Button disabled={retrying} onClick={onRetry} className="gap-2">
            {retrying ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <RefreshCw className="w-4 h-4" />
            )}
            {status === "segmented"
              ? "Generate 3D Model"
              : status === "error"
                ? "Retry Processing"
                : "Start Processing"}
          </Button>
        </div>
      )}
    </div>
  );
}
